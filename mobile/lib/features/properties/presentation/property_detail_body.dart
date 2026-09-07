import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../application/property_controller.dart';
import 'property_widgets.dart';

class PropertyDetailBody<T> extends StatefulWidget {
  const PropertyDetailBody({
    required this.load,
    required this.builder,
    super.key,
  });
  final Future<T> Function(bool) load;
  final Widget Function(BuildContext, T, Future<void> Function()) builder;
  @override
  State<PropertyDetailBody<T>> createState() => _PropertyDetailBodyState<T>();
}

class _PropertyDetailBodyState<T> extends State<PropertyDetailBody<T>> {
  late final PropertyController<T> _controller;
  @override
  void initState() {
    super.initState();
    _controller = PropertyController(widget.load)..load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: _controller,
    builder: (context, _) {
      final value = _controller.value;
      if (_controller.error != null) {
        return ListView(
          children: [
            PropertyFailure(
              _controller.error!,
              retry: () => _controller.load(force: true),
            ),
          ],
        );
      }
      if (value == null) return ListView(children: const [PropertyLoading()]);
      return RefreshIndicator(
        onRefresh: () => _controller.load(force: true),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(AqariSpacing.x4),
          children: [
            widget.builder(context, value, () => _controller.load(force: true)),
          ],
        ),
      );
    },
  );
}
