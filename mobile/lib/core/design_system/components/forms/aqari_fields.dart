import 'package:flutter/material.dart';

import '../../theme/aqari_color_scheme.dart';
import '../../theme/aqari_radius.dart';
import '../../theme/aqari_spacing.dart';

class AqariTextField extends StatelessWidget {
  const AqariTextField({
    this.controller,
    this.label,
    this.hint,
    this.helperText,
    this.errorText,
    this.enabled = true,
    this.obscureText = false,
    this.autofocus = false,
    this.keyboardType,
    this.textDirection,
    this.prefixIcon,
    this.suffix,
    this.onChanged,
    this.maxLines = 1,
    super.key,
  });
  final TextEditingController? controller;
  final String? label, hint, helperText, errorText;
  final bool enabled, obscureText, autofocus;
  final TextInputType? keyboardType;
  final TextDirection? textDirection;
  final IconData? prefixIcon;
  final Widget? suffix;
  final ValueChanged<String>? onChanged;
  final int maxLines;

  @override
  Widget build(BuildContext context) => ConstrainedBox(
    constraints: const BoxConstraints(minHeight: AqariSpacing.x12),
    child: TextField(
      controller: controller,
      enabled: enabled,
      obscureText: obscureText,
      autofocus: autofocus,
      keyboardType: keyboardType,
      textDirection: textDirection,
      onChanged: onChanged,
      maxLines: maxLines,
      style: Theme.of(
        context,
      ).textTheme.bodyLarge?.copyWith(color: context.aqariColors.textPrimary),
      decoration: InputDecoration(
        labelText: label,
        hintText: hint,
        helperText: helperText,
        errorText: errorText,
        errorMaxLines: 3,
        prefixIcon: prefixIcon == null ? null : Icon(prefixIcon, size: 20),
        suffixIcon: suffix,
      ),
    ),
  );
}

class AqariSearchField extends StatelessWidget {
  const AqariSearchField({
    required this.hint,
    this.onChanged,
    this.controller,
    super.key,
  });
  final String hint;
  final ValueChanged<String>? onChanged;
  final TextEditingController? controller;

  @override
  Widget build(BuildContext context) => SizedBox(
    height: 48,
    child: TextField(
      controller: controller,
      onChanged: onChanged,
      style: Theme.of(context).textTheme.bodyMedium,
      decoration: InputDecoration(
        hintText: hint,
        prefixIcon: const Icon(Icons.search_rounded, size: 20),
        contentPadding: const EdgeInsetsDirectional.symmetric(
          horizontal: AqariSpacing.x4,
        ),
        filled: true,
        fillColor: context.aqariColors.field,
        border: const OutlineInputBorder(borderRadius: AqariRadius.fullBorder),
        enabledBorder: OutlineInputBorder(
          borderRadius: AqariRadius.fullBorder,
          borderSide: BorderSide(color: context.aqariColors.outlineVariant),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: AqariRadius.fullBorder,
          borderSide: BorderSide(color: context.aqariColors.focus, width: 2),
        ),
      ),
    ),
  );
}

class AqariSelect<T> extends StatelessWidget {
  const AqariSelect({
    required this.label,
    required this.valueLabel,
    required this.items,
    required this.itemLabel,
    required this.onChanged,
    this.enabled = true,
    super.key,
  });
  final String label, valueLabel;
  final List<T> items;
  final String Function(T) itemLabel;
  final ValueChanged<T> onChanged;
  final bool enabled;

  Future<void> _show(BuildContext context) async {
    final selected = await showModalBottomSheet<T>(
      context: context,
      showDragHandle: true,
      builder: (sheetContext) => SafeArea(
        child: Padding(
          padding: const EdgeInsetsDirectional.fromSTEB(20, 8, 20, 24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(label, style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: AqariSpacing.x3),
              Flexible(
                child: ListView(
                  shrinkWrap: true,
                  children: items
                      .map(
                        (item) => InkWell(
                          borderRadius: AqariRadius.smBorder,
                          onTap: () => Navigator.pop(sheetContext, item),
                          child: ConstrainedBox(
                            constraints: const BoxConstraints(
                              minHeight: AqariSpacing.x12,
                            ),
                            child: Align(
                              alignment: AlignmentDirectional.centerStart,
                              child: Text(itemLabel(item)),
                            ),
                          ),
                        ),
                      )
                      .toList(growable: false),
                ),
              ),
            ],
          ),
        ),
      ),
    );
    if (selected != null) onChanged(selected);
  }

  @override
  Widget build(BuildContext context) => Semantics(
    button: true,
    label: label,
    value: valueLabel,
    enabled: enabled,
    child: InkWell(
      onTap: enabled ? () => _show(context) : null,
      borderRadius: AqariRadius.smBorder,
      child: Container(
        constraints: const BoxConstraints(minHeight: AqariSpacing.x12),
        padding: const EdgeInsetsDirectional.symmetric(
          horizontal: AqariSpacing.x4,
        ),
        decoration: BoxDecoration(
          color: enabled
              ? context.aqariColors.field
              : context.aqariColors.disabledContainer,
          borderRadius: AqariRadius.smBorder,
          border: Border.all(
            color: enabled
                ? context.aqariColors.outlineVariant
                : context.aqariColors.outlineVariant,
          ),
        ),
        child: Row(
          children: [
            Expanded(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    label,
                    style: Theme.of(context).textTheme.labelSmall?.copyWith(
                      color: context.aqariColors.textMuted,
                    ),
                  ),
                  Text(
                    valueLabel,
                    style: Theme.of(context).textTheme.bodyLarge,
                  ),
                ],
              ),
            ),
            Icon(
              Icons.expand_more_rounded,
              color: enabled
                  ? context.aqariColors.textSecondary
                  : context.aqariColors.textDisabled,
            ),
          ],
        ),
      ),
    ),
  );
}

class AqariCheckbox extends StatelessWidget {
  const AqariCheckbox({
    required this.label,
    required this.value,
    required this.onChanged,
    super.key,
  });
  final String label;
  final bool value;
  final ValueChanged<bool?>? onChanged;
  @override
  Widget build(BuildContext context) => Semantics(
    checked: value,
    enabled: onChanged != null,
    child: InkWell(
      onTap: onChanged == null ? null : () => onChanged!(!value),
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: AqariSpacing.x12),
        child: Row(
          children: [
            Checkbox(
              value: value,
              onChanged: onChanged,
              activeColor: context.aqariColors.primary,
              checkColor: context.aqariColors.onPrimary,
              side: BorderSide(color: context.aqariColors.outline),
              shape: const RoundedRectangleBorder(
                borderRadius: AqariRadius.xsBorder,
              ),
            ),
            Expanded(child: Text(label)),
          ],
        ),
      ),
    ),
  );
}

class AqariRadio<T> extends StatelessWidget {
  const AqariRadio({
    required this.label,
    required this.value,
    required this.groupValue,
    required this.onChanged,
    super.key,
  });
  final String label;
  final T value, groupValue;
  final ValueChanged<T?>? onChanged;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onChanged == null ? null : () => onChanged!(value),
    child: ConstrainedBox(
      constraints: const BoxConstraints(minHeight: AqariSpacing.x12),
      child: Row(
        children: [
          Radio<T>(
            value: value,
            groupValue: groupValue,
            onChanged: onChanged,
            activeColor: context.aqariColors.primary,
          ),
          Expanded(child: Text(label)),
        ],
      ),
    ),
  );
}

class AqariSwitch extends StatelessWidget {
  const AqariSwitch({
    required this.label,
    required this.value,
    required this.onChanged,
    super.key,
  });
  final String label;
  final bool value;
  final ValueChanged<bool>? onChanged;
  @override
  Widget build(BuildContext context) => ConstrainedBox(
    constraints: const BoxConstraints(minHeight: AqariSpacing.x12),
    child: Row(
      children: [
        Expanded(child: Text(label)),
        Switch(
          value: value,
          onChanged: onChanged,
          activeColor: context.aqariColors.onPrimary,
          activeTrackColor: context.aqariColors.primary,
          inactiveThumbColor: context.aqariColors.textMuted,
          inactiveTrackColor: context.aqariColors.surfaceContainer,
        ),
      ],
    ),
  );
}
