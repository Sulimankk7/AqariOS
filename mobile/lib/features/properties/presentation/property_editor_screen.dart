import 'package:flutter/material.dart';
import 'package:latlong2/latlong.dart';
import '../../../core/design_system/design_system.dart';
import '../../../core/network/api_problem.dart';
import '../../auth/domain/user_profile.dart';
import '../data/properties_repository.dart';
import '../domain/property_models.dart';
import '../domain/property_form.dart';
import 'property_widgets.dart';
import 'building_location_picker.dart';

class PropertyEditorScreen extends StatefulWidget {
  const PropertyEditorScreen({
    required this.repository,
    required this.user,
    required this.kind,
    this.record,
    this.parentId,
    this.parentLabel,
    super.key,
  });
  final PropertiesRepository repository;
  final UserProfile user;
  final PropertyKind kind;
  final PropertyRecord? record;
  final String? parentId, parentLabel;
  @override
  State<PropertyEditorScreen> createState() => _PropertyEditorScreenState();
}

class _PropertyEditorScreenState extends State<PropertyEditorScreen> {
  final _inputs = <String, TextEditingController>{};
  final _errors = <String, String>{};
  final _manuallyEditedFields = <String>{};
  int _type = 0, _governorate = 0, _ownership = 0;
  String _currency = 'JOD';
  bool _saving = false, _attempted = false;
  bool _geocoding = false, _geocodingFailed = false;
  int _geocodingSequence = 0;
  String? _suggestedAddress;
  ApiProblem? _problem;
  bool get _editing => widget.record != null;
  List<PropertyField> get _fields =>
      propertyFields(widget.kind, editing: _editing, ownership: _ownership);
  Map<String, String> get _values => {
    for (final e in _inputs.entries) e.key: e.value.text,
  };
  @override
  void initState() {
    super.initState();
    final record = widget.record;
    if (record is Building) {
      _type = record.type;
      _governorate = record.address?.governorate ?? 0;
    }
    if (record is Floor) _type = record.type;
    if (record == null && widget.kind == PropertyKind.floors) _type = 2;
    if (record is Apartment) {
      _ownership = record.ownership;
      _currency = record.currency;
    }
    final initial = initialPropertyValues(record);
    for (final f in propertyFields(
      widget.kind,
      editing: _editing,
      ownership: 1,
    )) {
      _inputs[f.key] = TextEditingController(text: initial[f.key] ?? '');
    }
    if (_editing && widget.kind == PropertyKind.buildings) {
      _manuallyEditedFields.add('addressGovernorate');
      for (final key in const [
        'addressCity',
        'addressNeighborhood',
        'addressStreet',
        'addressPostalCode',
      ]) {
        if (_inputs[key]!.text.trim().isNotEmpty) {
          _manuallyEditedFields.add(key);
        }
      }
    }
    _loadSuggestion();
  }

  double? get _latitude =>
      double.tryParse(normalizeNumber(_inputs['gpsLatitude']?.text ?? ''));
  double? get _longitude =>
      double.tryParse(normalizeNumber(_inputs['gpsLongitude']?.text ?? ''));

  Future<void> _selectBuildingLocation(LatLng point) async {
    final latitude = double.parse(point.latitude.toStringAsFixed(6));
    final longitude = double.parse(point.longitude.toStringAsFixed(6));
    _inputs['gpsLatitude']!.text = latitude.toString();
    _inputs['gpsLongitude']!.text = longitude.toString();
    final sequence = ++_geocodingSequence;
    setState(() {
      _geocoding = true;
      _geocodingFailed = false;
      _suggestedAddress = null;
    });
    try {
      final result = await widget.repository.reverseGeocode(
        latitude,
        longitude,
        context.isArabic ? 'ar' : 'en',
      );
      if (!mounted ||
          !isCurrentLocationResponse(sequence, _geocodingSequence)) {
        return;
      }
      _mergeAddressSuggestion(result);
      setState(() => _suggestedAddress = result.formattedAddress);
    } catch (_) {
      if (!mounted ||
          !isCurrentLocationResponse(sequence, _geocodingSequence)) {
        return;
      }
      setState(() => _geocodingFailed = true);
    } finally {
      if (mounted && isCurrentLocationResponse(sequence, _geocodingSequence)) {
        setState(() => _geocoding = false);
      }
    }
  }

  void _mergeAddressSuggestion(ReverseGeocodingResult result) {
    final values = <String, String?>{
      'addressCity': result.city ?? result.district,
      'addressNeighborhood': result.neighborhood,
      'addressStreet': result.street,
      'addressPostalCode': result.postalCode,
    };
    for (final entry in values.entries) {
      final value = entry.value?.trim();
      if (value != null &&
          value.isNotEmpty &&
          !_manuallyEditedFields.contains(entry.key)) {
        _inputs[entry.key]!.text = value;
      }
    }
    final governorate = _jordanGovernorate(result);
    if (governorate != null &&
        !_manuallyEditedFields.contains('addressGovernorate')) {
      _governorate = governorate;
    }
  }

  int? _jordanGovernorate(ReverseGeocodingResult result) {
    if (result.countryCode?.toUpperCase() != 'JO') return null;
    final value = result.governorate
        ?.toLowerCase()
        .replaceAll(RegExp(r' governorate$'), '')
        .trim();
    const names = {
      'amman': 0,
      'عمان': 0,
      'عمّان': 0,
      'zarqa': 1,
      'الزرقاء': 1,
      'irbid': 2,
      'إربد': 2,
      'اربد': 2,
      'balqa': 3,
      'البلقاء': 3,
      'madaba': 4,
      'مادبا': 4,
      'مأدبا': 4,
      'karak': 5,
      'الكرك': 5,
      'tafilah': 6,
      'الطفيلة': 6,
      "ma'an": 7,
      'maan': 7,
      'معان': 7,
      'aqaba': 8,
      'العقبة': 8,
      'ajloun': 9,
      'عجلون': 9,
      'jerash': 10,
      'جرش': 10,
      'mafraq': 11,
      'المفرق': 11,
    };
    return names[value];
  }

  Future<void> _loadSuggestion() async {
    if (_editing) return;
    final (field, suggestion) = switch (widget.kind) {
      PropertyKind.buildings => (
        'internalCode',
        widget.repository.nextBuildingCode(),
      ),
      PropertyKind.floors when widget.parentId != null => (
        'floorNumber',
        widget.repository.nextFloorNumber(widget.parentId!),
      ),
      PropertyKind.apartments when widget.parentId != null => (
        'unitNumber',
        widget.repository.nextApartmentNumber(widget.parentId!),
      ),
      _ => (null, null),
    };
    if (field == null || suggestion == null) return;
    try {
      final value = await suggestion;
      if (!mounted ||
          _manuallyEditedFields.contains(field) ||
          _inputs[field]!.text.isNotEmpty) {
        return;
      }
      _inputs[field]!.text = value;
    } catch (_) {
      // Suggestions are optional convenience. Manual entry remains available.
    }
  }

  @override
  void dispose() {
    for (final controller in _inputs.values) {
      controller.dispose();
    }
    super.dispose();
  }

  bool _validate() {
    _errors.clear();
    for (final field in _fields) {
      final error = field.validate(_inputs[field.key]!.text, context.isArabic);
      if (error != null) _errors[field.key] = error;
    }
    if (widget.kind == PropertyKind.buildings) {
      final lat = _inputs['gpsLatitude']!.text.trim().isEmpty;
      final lon = _inputs['gpsLongitude']!.text.trim().isEmpty;
      if (lat != lon) {
        final message = tr(
          context,
          'أدخل الإحداثيين معاً أو اتركهما فارغين.',
          'Provide both coordinates or leave both empty.',
        );
        _errors['gpsLatitude'] = message;
        _errors['gpsLongitude'] = message;
      }
      if (_type < 0 || _type > 2 || _governorate < 0 || _governorate > 11) {
        _errors['selection'] = tr(
          context,
          'اختر نوعاً ومحافظة صالحين.',
          'Select a valid type and governorate.',
        );
      }
    }
    if (widget.kind == PropertyKind.floors && (_type < 0 || _type > 3)) {
      _errors['selection'] = tr(
        context,
        'اختر نوع طابق صالحاً.',
        'Select a valid floor type.',
      );
    }
    if (widget.kind == PropertyKind.apartments &&
        !const ['JOD', 'USD', 'EUR', 'AED', 'SAR'].contains(_currency)) {
      _errors['selection'] = tr(
        context,
        'اختر عملة مدعومة.',
        'Select a supported currency.',
      );
    }
    return _errors.isEmpty;
  }

  Future<void> _save() async {
    if (_saving ||
        !propertyPermission(widget.user, _editing ? 'update' : 'create')) {
      return;
    }
    _attempted = true;
    if (!_validate()) {
      setState(() {});
      return;
    }
    FocusScope.of(context).unfocus();
    setState(() {
      _saving = true;
      _problem = null;
    });
    try {
      final PropertyWriteRequest request = switch (widget.kind) {
        PropertyKind.buildings => BuildingWriteRequest(
          _values,
          type: _type,
          governorate: _governorate,
          editing: _editing,
        ),
        PropertyKind.floors => FloorWriteRequest(
          _values,
          type: _type,
          editing: _editing,
        ),
        PropertyKind.apartments => ApartmentWriteRequest(
          _values,
          editing: _editing,
          ownership: _ownership,
          currency: _currency,
        ),
      };
      final path = _editing
          ? '/api/v1/${widget.kind.name}/${Uri.encodeComponent(widget.record!.id)}'
          : switch (widget.kind) {
              PropertyKind.buildings => '/api/v1/buildings',
              PropertyKind.floors =>
                '/api/v1/buildings/${Uri.encodeComponent(widget.parentId!)}/floors',
              PropertyKind.apartments =>
                '/api/v1/floors/${Uri.encodeComponent(widget.parentId!)}/apartments',
            };
      await widget.repository.save(path, request.toJson(), editing: _editing);
      if (mounted) {
        AqariSnackbar.show(
          context,
          tr(context, 'تم الحفظ', 'Saved'),
          kind: AqariStatusKind.success,
        );
        Navigator.pop(context, true);
      }
    } on ApiProblem catch (error) {
      if (!mounted) return;
      setState(() {
        _problem = error;
        // Keep field association; never render arbitrary backend details or stack traces.
        for (final key in error.validationErrors.keys) {
          final normalized = key
              .replaceAll(r'$.', '')
              .split('.')
              .last
              .toLowerCase();
          for (final field in _fields) {
            if (field.key.toLowerCase() == normalized) {
              _errors[field.key] = tr(
                context,
                'رفض الخادم هذه القيمة. راجع الحقل.',
                'The server rejected this value. Review this field.',
              );
            }
          }
        }
      });
    } catch (_) {
      if (mounted) {
        setState(() => _problem = const ApiProblem(message: 'Unable to save'));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !_saving,
    child: Scaffold(
      appBar: AqariAppBar(
        title:
            '${_editing ? tr(context, 'تعديل', 'Edit') : tr(context, 'إضافة', 'Add')} · ${kindLabel(context, widget.kind)}',
        onBack: _saving ? null : () => Navigator.maybePop(context),
      ),
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(AqariSpacing.x4),
                keyboardDismissBehavior:
                    ScrollViewKeyboardDismissBehavior.onDrag,
                children: [
                  if (widget.parentLabel != null)
                    Text(
                      widget.parentLabel!,
                      style: Theme.of(context).textTheme.titleSmall,
                    ),
                  if (_editing)
                    Padding(
                      padding: const EdgeInsets.only(bottom: AqariSpacing.x4),
                      child: Text(
                        widget.kind == PropertyKind.apartments
                            ? tr(
                                context,
                                'يسمح النظام بتعديل الإيجار والعملة فقط.',
                                'Only base rent and currency can be edited.',
                              )
                            : widget.kind == PropertyKind.floors
                            ? tr(
                                context,
                                'رقم الطابق ثابت بعد الإنشاء.',
                                'Floor number cannot be changed after creation.',
                              )
                            : tr(
                                context,
                                'عدد الطوابق المعلن ثابت بعد الإنشاء.',
                                'Declared floor count cannot be changed after creation.',
                              ),
                      ),
                    ),
                  if (_problem != null) PropertyFailure(_problem!),
                  if (_errors['selection'] != null)
                    Text(
                      _errors['selection']!,
                      style: TextStyle(color: context.aqariColors.error),
                    ),
                  ..._formSections(),
                ],
              ),
            ),
            AqariActionBar(
              child: AqariButton(
                label: tr(context, 'حفظ', 'Save'),
                expanded: true,
                loading: _saving,
                onPressed:
                    propertyPermission(
                      widget.user,
                      _editing ? 'update' : 'create',
                    )
                    ? _save
                    : null,
              ),
            ),
          ],
        ),
      ),
    ),
  );

  List<Widget> _formSections() {
    List<Widget> fields(List<String> keys) => [
      for (final field in _fields)
        if (keys.contains(field.key)) _field(field),
    ];
    if (widget.kind == PropertyKind.buildings) {
      return [
        AqariFormSection(
          title: tr(context, 'بيانات المبنى', 'Building information'),
          children: [
            ...fields(['name', 'internalCode', 'totalFloors']),
            _select(
              tr(context, 'نوع المبنى', 'Building type'),
              _type,
              [0, 1, 2],
              (v) => buildingTypeLabel(context, v),
              (v) => _type = v,
            ),
          ],
        ),
        AqariFormSection(
          title: tr(context, 'العنوان', 'Address'),
          children: [
            _select(
              tr(context, 'المحافظة', 'Governorate'),
              _governorate,
              List.generate(12, (i) => i),
              (v) => context.isArabic
                  ? [
                      'عمّان',
                      'الزرقاء',
                      'إربد',
                      'البلقاء',
                      'مادبا',
                      'الكرك',
                      'الطفيلة',
                      'معان',
                      'العقبة',
                      'عجلون',
                      'جرش',
                      'المفرق',
                    ][v]
                  : governorateNames[v],
              (v) => _governorate = v,
              fieldKey: 'addressGovernorate',
            ),
            ...fields([
              'addressCity',
              'addressNeighborhood',
              'addressStreet',
              'addressPostalCode',
            ]),
          ],
        ),
        AqariFormSection(
          title: tr(context, 'الموقع', 'Location'),
          children: [
            Text(
              'قد تكون بعض تفاصيل العنوان غير دقيقة. يرجى التحقق من العنوان والموقع على الخريطة.',
              textDirection: TextDirection.rtl,
              semanticsLabel:
                  'قد تكون بعض تفاصيل العنوان غير دقيقة. يرجى التحقق من العنوان والموقع على الخريطة.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: context.aqariColors.textSecondary,
              ),
            ),
            const SizedBox(height: AqariSpacing.x3),
            BuildingLocationPicker(
              latitude: _latitude,
              longitude: _longitude,
              onSelected: _selectBuildingLocation,
            ),
            if (_geocoding)
              Padding(
                padding: const EdgeInsets.only(top: AqariSpacing.x3),
                child: Text(
                  tr(
                    context,
                    'جارٍ اقتراح العنوان…',
                    'Finding a suggested address…',
                  ),
                ),
              ),
            if (_geocodingFailed)
              Padding(
                padding: const EdgeInsets.only(top: AqariSpacing.x3),
                child: Text(
                  tr(
                    context,
                    'تعذر اقتراح العنوان. يمكنك إدخاله يدوياً.',
                    'The address could not be suggested. You can enter it manually.',
                  ),
                  style: TextStyle(color: context.aqariColors.warning),
                ),
              ),
            if (_suggestedAddress?.trim().isNotEmpty == true)
              Padding(
                padding: const EdgeInsets.only(top: AqariSpacing.x3),
                child: Text(
                  _suggestedAddress!,
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ),
          ],
        ),
        AqariFormSection(
          title: tr(context, 'تفاصيل إضافية', 'Additional details'),
          children: fields(['constructionYear', 'gpsLatitude', 'gpsLongitude']),
        ),
      ];
    }
    if (widget.kind == PropertyKind.floors) {
      return [
        AqariFormSection(
          title: tr(context, 'بيانات الطابق', 'Floor information'),
          children: [
            ...fields(['floorNumber', 'floorLabel']),
            _select(
              tr(context, 'نوع الطابق', 'Floor type'),
              _type,
              [0, 1, 2, 3],
              (v) => floorTypeLabel(context, v),
              (v) => _type = v,
            ),
          ],
        ),
      ];
    }
    return [
      if (!_editing) ...[
        AqariFormSection(
          title: tr(context, 'مواصفات الوحدة', 'Unit information'),
          children: fields(['unitNumber', 'areaSqm', 'bedrooms', 'bathrooms']),
        ),
        AqariFormSection(
          title: tr(context, 'الملكية', 'Ownership'),
          children: [
            _select(
              tr(context, 'الملكية', 'Ownership'),
              _ownership,
              [0, 1],
              (v) => v == 0
                  ? tr(context, 'ملك الشركة', 'Company owned')
                  : tr(context, 'ملك طرف ثالث', 'Third-party owned'),
              (v) => _ownership = v,
            ),
            ...fields(['externalOwnerName', 'externalOwnerPhone']),
          ],
        ),
      ],
      AqariFormSection(
        title: tr(context, 'الإيجار', 'Rent'),
        children: [
          ...fields(['baseRentAmount']),
          AqariSelect<String>(
            label: tr(context, 'العملة', 'Currency'),
            valueLabel: _currency,
            items: const ['JOD', 'USD', 'EUR', 'AED', 'SAR'],
            itemLabel: (v) => v,
            enabled: !_saving,
            onChanged: (v) => setState(() => _currency = v),
          ),
        ],
      ),
    ];
  }

  Widget _field(PropertyField field) => Padding(
    padding: const EdgeInsets.only(bottom: AqariSpacing.x3),
    child: AqariTextField(
      key: ValueKey(field.key),
      controller: _inputs[field.key],
      label: context.isArabic ? field.ar : field.en,
      errorText: _errors[field.key],
      enabled: !_saving,
      textDirection: field.numeric || field.ltr ? TextDirection.ltr : null,
      keyboardType: field.numeric
          ? TextInputType.numberWithOptions(
              decimal: !field.integer,
              signed: field.min! < 0,
            )
          : field.key == 'externalOwnerPhone'
          ? TextInputType.phone
          : null,
      onChanged: (_) {
        _manuallyEditedFields.add(field.key);
        if (field.key == 'gpsLatitude' || field.key == 'gpsLongitude') {
          setState(() {
            if (_attempted) _validate();
          });
        } else if (_attempted) {
          setState(_validate);
        }
      },
    ),
  );

  Widget _select(
    String label,
    int value,
    List<int> options,
    String Function(int) text,
    void Function(int) update, {
    String? fieldKey,
  }) => Padding(
    padding: const EdgeInsets.only(bottom: AqariSpacing.x3),
    child: AqariSelect<int>(
      label: label,
      valueLabel: options.contains(value)
          ? text(value)
          : tr(context, 'اختر قيمة', 'Select a value'),
      items: options,
      itemLabel: text,
      enabled: !_saving,
      onChanged: (v) => setState(() {
        if (fieldKey != null) _manuallyEditedFields.add(fieldKey);
        update(v);
        if (_attempted) _validate();
      }),
    ),
  );
}
