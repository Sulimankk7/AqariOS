import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../core/design_system/design_system.dart';

class AuthScaffold extends StatelessWidget {
  const AuthScaffold({
    required this.title,
    required this.subtitle,
    required this.child,
    this.onBack,
    this.centered = false,
    super.key,
  });
  final String title, subtitle;
  final Widget child;
  final VoidCallback? onBack;
  final bool centered;

  @override
  Widget build(BuildContext context) => Scaffold(
    body: SafeArea(
      child: LayoutBuilder(
        builder: (context, constraints) => SingleChildScrollView(
          keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
          padding: EdgeInsets.fromLTRB(
            AqariSpacing.page,
            AqariSpacing.x3,
            AqariSpacing.page,
            MediaQuery.viewInsetsOf(context).bottom + AqariSpacing.x5,
          ),
          child: ConstrainedBox(
            constraints: BoxConstraints(
              minHeight: constraints.maxHeight - AqariSpacing.x8,
            ),
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 440),
                child: Column(
                  mainAxisAlignment: centered
                      ? MainAxisAlignment.center
                      : MainAxisAlignment.start,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Align(
                      alignment: AlignmentDirectional.centerStart,
                      child: AqariLogo(size: 44),
                    ),
                    if (onBack != null)
                      Align(
                        alignment: AlignmentDirectional.centerStart,
                        child: AqariIconButton(
                          icon: Icons.arrow_back_rounded,
                          semanticLabel: context.isArabic ? 'رجوع' : 'Back',
                          onPressed: onBack,
                        ),
                      ),
                    const SizedBox(height: AqariSpacing.x3),
                    Text(
                      title,
                      textAlign: centered ? TextAlign.center : TextAlign.start,
                      style: Theme.of(context).textTheme.headlineMedium,
                    ),
                    const SizedBox(height: AqariSpacing.x2),
                    Text(
                      subtitle,
                      textAlign: centered ? TextAlign.center : TextAlign.start,
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: context.aqariColors.textSecondary,
                        height: 1.5,
                      ),
                    ),
                    const SizedBox(height: AqariSpacing.x5),
                    child,
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    ),
  );
}

class AuthSegmentedControl extends StatelessWidget {
  const AuthSegmentedControl({
    required this.items,
    required this.selected,
    required this.onChanged,
    super.key,
  });
  final List<(String, IconData)> items;
  final int selected;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(AqariSpacing.x1),
    decoration: BoxDecoration(
      color: context.aqariColors.surfaceLow,
      borderRadius: AqariRadius.fullBorder,
      border: Border.all(color: context.aqariColors.outline),
    ),
    child: Row(
      children: [
        for (final (index, item) in items.indexed)
          Expanded(
            child: Semantics(
              selected: index == selected,
              button: true,
              child: InkWell(
                borderRadius: AqariRadius.fullBorder,
                onTap: () => onChanged(index),
                child: AnimatedContainer(
                  duration: AqariMotion.fast,
                  constraints: const BoxConstraints(minHeight: 44),
                  decoration: BoxDecoration(
                    color: index == selected
                        ? context.aqariColors.primary
                        : Colors.transparent,
                    borderRadius: AqariRadius.fullBorder,
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(item.$2, size: 17),
                      const SizedBox(width: AqariSpacing.x2),
                      Flexible(child: Text(item.$1)),
                    ],
                  ),
                ),
              ),
            ),
          ),
      ],
    ),
  );
}

class AuthTextField extends StatelessWidget {
  const AuthTextField({
    required this.controller,
    required this.label,
    this.hint,
    this.icon,
    this.errorText,
    this.enabled = true,
    this.keyboardType,
    this.obscureText = false,
    this.suffix,
    this.onChanged,
    this.inputFormatters,
    this.autofillHints,
    super.key,
  });
  final TextEditingController controller;
  final String label;
  final String? hint, errorText;
  final IconData? icon;
  final bool enabled, obscureText;
  final TextInputType? keyboardType;
  final Widget? suffix;
  final ValueChanged<String>? onChanged;
  final List<TextInputFormatter>? inputFormatters;
  final Iterable<String>? autofillHints;

  @override
  Widget build(BuildContext context) => TextField(
    controller: controller,
    enabled: enabled,
    obscureText: obscureText,
    keyboardType: keyboardType,
    textDirection:
        keyboardType == TextInputType.phone ||
            keyboardType == TextInputType.emailAddress
        ? TextDirection.ltr
        : null,
    inputFormatters: inputFormatters,
    autofillHints: autofillHints,
    onChanged: onChanged,
    decoration: InputDecoration(
      labelText: label,
      hintText: hint,
      errorText: errorText,
      errorMaxLines: 3,
      prefixIcon: icon == null ? null : Icon(icon, size: 19),
      suffixIcon: suffix,
      border: const OutlineInputBorder(borderRadius: AqariRadius.fullBorder),
      enabledBorder: OutlineInputBorder(
        borderRadius: AqariRadius.fullBorder,
        borderSide: BorderSide(color: context.aqariColors.outline),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: AqariRadius.fullBorder,
        borderSide: BorderSide(color: context.aqariColors.focus, width: 2),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: AqariRadius.fullBorder,
        borderSide: BorderSide(color: context.aqariColors.error),
      ),
      focusedErrorBorder: OutlineInputBorder(
        borderRadius: AqariRadius.fullBorder,
        borderSide: BorderSide(color: context.aqariColors.error, width: 2),
      ),
    ),
  );
}

class AuthPasswordField extends StatefulWidget {
  const AuthPasswordField({
    required this.controller,
    required this.label,
    this.errorText,
    this.enabled = true,
    this.onChanged,
    super.key,
  });
  final TextEditingController controller;
  final String label;
  final String? errorText;
  final bool enabled;
  final ValueChanged<String>? onChanged;
  @override
  State<AuthPasswordField> createState() => _AuthPasswordFieldState();
}

class _AuthPasswordFieldState extends State<AuthPasswordField> {
  bool hidden = true;
  @override
  Widget build(BuildContext context) => AuthTextField(
    controller: widget.controller,
    label: widget.label,
    icon: Icons.lock_outline_rounded,
    obscureText: hidden,
    enabled: widget.enabled,
    errorText: widget.errorText,
    onChanged: widget.onChanged,
    autofillHints: const [AutofillHints.password],
    suffix: AqariIconButton(
      icon: hidden ? Icons.visibility_outlined : Icons.visibility_off_outlined,
      semanticLabel: hidden
          ? (context.isArabic ? 'إظهار كلمة المرور' : 'Show password')
          : (context.isArabic ? 'إخفاء كلمة المرور' : 'Hide password'),
      onPressed: widget.enabled ? () => setState(() => hidden = !hidden) : null,
    ),
  );
}

class AuthBanner extends StatelessWidget {
  const AuthBanner({
    required this.message,
    this.kind = AuthBannerKind.error,
    super.key,
  });
  final String message;
  final AuthBannerKind kind;
  @override
  Widget build(BuildContext context) {
    final (foreground, background, icon) = switch (kind) {
      AuthBannerKind.error => (
        context.aqariColors.error,
        context.aqariColors.errorContainer,
        Icons.error_outline_rounded,
      ),
      AuthBannerKind.success => (
        context.aqariColors.success,
        context.aqariColors.successContainer,
        Icons.check_circle_outline_rounded,
      ),
      AuthBannerKind.info => (
        context.aqariColors.info,
        context.aqariColors.infoContainer,
        Icons.info_outline_rounded,
      ),
    };
    return Semantics(
      liveRegion: true,
      child: Container(
        padding: const EdgeInsets.all(AqariSpacing.x3),
        decoration: BoxDecoration(
          color: background,
          borderRadius: AqariRadius.mdBorder,
          border: Border.all(color: foreground.withValues(alpha: .45)),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: foreground, size: 20),
            const SizedBox(width: AqariSpacing.x2),
            Expanded(
              child: Text(message, style: TextStyle(color: foreground)),
            ),
          ],
        ),
      ),
    );
  }
}

enum AuthBannerKind { error, success, info }

class AuthOtpInput extends StatefulWidget {
  const AuthOtpInput({required this.onChanged, this.enabled = true, super.key});
  final ValueChanged<String> onChanged;
  final bool enabled;
  @override
  State<AuthOtpInput> createState() => _AuthOtpInputState();
}

class _AuthOtpInputState extends State<AuthOtpInput> {
  late final controllers = List.generate(6, (_) => TextEditingController());
  late final nodes = List.generate(6, (_) => FocusNode());
  @override
  void dispose() {
    for (final c in controllers) {
      c.dispose();
    }
    for (final n in nodes) {
      n.dispose();
    }
    super.dispose();
  }

  void changed(int index, String value) {
    if (value.length > 1) {
      final digits = value
          .replaceAll(RegExp(r'\D'), '')
          .split('')
          .take(6)
          .toList();
      for (var i = 0; i < 6; i++) {
        controllers[i].text = i < digits.length ? digits[i] : '';
      }
      nodes[digits.length.clamp(0, 5)].requestFocus();
    } else if (value.isNotEmpty && index < 5) {
      nodes[index + 1].requestFocus();
    }
    widget.onChanged(controllers.map((c) => c.text).join());
  }

  @override
  Widget build(BuildContext context) => Directionality(
    textDirection: TextDirection.ltr,
    child: Row(
      children: [
        for (var i = 0; i < 6; i++)
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(right: i == 5 ? 0 : AqariSpacing.x2),
              child: Semantics(
                label:
                    '${context.isArabic ? 'رقم الرمز' : 'Code digit'} ${i + 1}',
                child: TextField(
                  controller: controllers[i],
                  focusNode: nodes[i],
                  enabled: widget.enabled,
                  autofocus: i == 0,
                  keyboardType: TextInputType.number,
                  textInputAction: i == 5
                      ? TextInputAction.done
                      : TextInputAction.next,
                  textAlign: TextAlign.center,
                  maxLength: 6,
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                  onChanged: (v) => changed(i, v),
                  decoration: const InputDecoration(
                    counterText: '',
                    contentPadding: EdgeInsets.symmetric(vertical: 16),
                    border: OutlineInputBorder(
                      borderRadius: AqariRadius.mdBorder,
                    ),
                  ),
                ),
              ),
            ),
          ),
      ],
    ),
  );
}

class AuthLink extends StatelessWidget {
  const AuthLink({required this.label, required this.onPressed, super.key});
  final String label;
  final VoidCallback? onPressed;
  @override
  Widget build(BuildContext context) => TextButton(
    onPressed: onPressed,
    style: TextButton.styleFrom(
      foregroundColor: context.aqariColors.secondary,
      minimumSize: const Size(48, 48),
    ),
    child: Text(label, style: const TextStyle(fontWeight: FontWeight.w700)),
  );
}
