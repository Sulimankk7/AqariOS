import 'package:flutter/material.dart';

import '../../localization/directional_content.dart';
import '../../theme/aqari_color_scheme.dart';
import '../../theme/aqari_radius.dart';
import '../../theme/aqari_spacing.dart';
import '../actions/aqari_button.dart';

class AqariAppBar extends StatelessWidget implements PreferredSizeWidget {
  const AqariAppBar({
    required this.title,
    this.onBack,
    this.actions = const [],
    super.key,
  });
  final String title;
  final VoidCallback? onBack;
  final List<Widget> actions;
  @override
  Size get preferredSize => const Size.fromHeight(56);
  @override
  Widget build(BuildContext context) => DecoratedBox(
    decoration: BoxDecoration(
      color: context.aqariColors.background,
      border: Border(
        bottom: BorderSide(color: context.aqariColors.borderMuted),
      ),
    ),
    child: SafeArea(
      bottom: false,
      child: SizedBox(
        height: 56,
        child: Padding(
          padding: const EdgeInsetsDirectional.symmetric(
            horizontal: AqariSpacing.x3,
          ),
          child: Row(
            children: [
              if (onBack != null)
                Semantics(
                  button: true,
                  label: MaterialLocalizations.of(context).backButtonTooltip,
                  child: InkWell(
                    onTap: onBack,
                    customBorder: const CircleBorder(),
                    child: const SizedBox.square(
                      dimension: 48,
                      child: Center(
                        child: AqariDirectionalIcon(
                          icon: Icons.arrow_back_rounded,
                        ),
                      ),
                    ),
                  ),
                ),
              Expanded(
                child: Text(
                  title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
              ...actions,
            ],
          ),
        ),
      ),
    ),
  );
}

class AqariNavigationDestination {
  const AqariNavigationDestination({required this.label, required this.icon});
  final String label;
  final IconData icon;
}

class AqariBottomNavigation extends StatelessWidget {
  const AqariBottomNavigation({
    required this.items,
    required this.selectedIndex,
    required this.onSelected,
    super.key,
  }) : assert(items.length <= 5);
  final List<AqariNavigationDestination> items;
  final int selectedIndex;
  final ValueChanged<int> onSelected;
  @override
  Widget build(BuildContext context) => SafeArea(
    top: false,
    child: Container(
      constraints: const BoxConstraints(minHeight: 68),
      decoration: BoxDecoration(
        color: context.aqariColors.card,
        border: Border(
          top: BorderSide(color: context.aqariColors.outlineVariant),
        ),
      ),
      child: Row(
        children: [
          for (var i = 0; i < items.length; i++)
            Expanded(
              child: _Destination(
                item: items[i],
                selected: selectedIndex == i,
                onTap: () => onSelected(i),
              ),
            ),
        ],
      ),
    ),
  );
}

class _Destination extends StatelessWidget {
  const _Destination({
    required this.item,
    required this.selected,
    required this.onTap,
  });
  final AqariNavigationDestination item;
  final bool selected;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) {
    final c = context.aqariColors;
    final color = selected ? c.buttonOnPrimary : c.textMuted;
    return Semantics(
      selected: selected,
      button: true,
      label: item.label,
      child: InkWell(
        onTap: onTap,
        child: ConstrainedBox(
          constraints: const BoxConstraints(minHeight: 68),
          // Scaffold gives the bottom bar the full available height as a maximum.
          // Shrink-wrap the items so the bar does not consume the dashboard body.
          child: Column(
            mainAxisSize: MainAxisSize.min,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              AnimatedContainer(
                duration: const Duration(milliseconds: 150),
                padding: const EdgeInsetsDirectional.symmetric(
                  horizontal: 14,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: selected ? c.buttonPrimary : Colors.transparent,
                  borderRadius: AqariRadius.fullBorder,
                ),
                child: Icon(item.icon, size: 20, color: color),
              ),
              const SizedBox(height: AqariSpacing.x1),
              Text(
                item.label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(
                  context,
                ).textTheme.labelSmall?.copyWith(color: color),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class AqariTabs extends StatelessWidget {
  const AqariTabs({
    required this.labels,
    required this.selectedIndex,
    required this.onSelected,
    super.key,
  });
  final List<String> labels;
  final int selectedIndex;
  final ValueChanged<int> onSelected;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: AqariSpacing.x2),
    child: SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Container(
        padding: const EdgeInsets.all(3),
        decoration: BoxDecoration(
          color: context.aqariColors.surfaceLow,
          border: Border.all(color: context.aqariColors.outlineVariant),
          borderRadius: AqariRadius.fullBorder,
        ),
        child: Row(
          children: [
            for (var i = 0; i < labels.length; i++)
              Semantics(
                selected: i == selectedIndex,
                button: true,
                child: InkWell(
                  onTap: () => onSelected(i),
                  child: Container(
                    constraints: const BoxConstraints(minHeight: 40),
                    padding: const EdgeInsetsDirectional.symmetric(
                      horizontal: AqariSpacing.x4,
                    ),
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: i == selectedIndex
                          ? context.aqariColors.buttonPrimary
                          : Colors.transparent,
                      borderRadius: AqariRadius.fullBorder,
                    ),
                    child: Text(
                      labels[i],
                      style: Theme.of(context).textTheme.labelLarge?.copyWith(
                        color: i == selectedIndex
                            ? context.aqariColors.buttonOnPrimary
                            : context.aqariColors.textSecondary,
                      ),
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    ),
  );
}

class AqariPopupMenuAction<T> {
  const AqariPopupMenuAction({
    required this.value,
    required this.label,
    this.icon,
  });
  final T value;
  final String label;
  final IconData? icon;
}

class AqariPopupMenu<T> extends StatelessWidget {
  const AqariPopupMenu({
    required this.label,
    required this.items,
    required this.onSelected,
    super.key,
  });
  final String label;
  final List<AqariPopupMenuAction<T>> items;
  final ValueChanged<T> onSelected;
  @override
  Widget build(BuildContext context) => AqariIconButton(
    icon: Icons.more_vert_rounded,
    semanticLabel: label,
    onPressed: () async {
      final selected = await showModalBottomSheet<T>(
        context: context,
        showDragHandle: true,
        builder: (sheetContext) => SafeArea(
          child: Padding(
            padding: const EdgeInsetsDirectional.fromSTEB(
              AqariSpacing.page,
              0,
              AqariSpacing.page,
              AqariSpacing.x5,
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(label, style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: AqariSpacing.x3),
                for (final item in items)
                  ListTile(
                    minTileHeight: 52,
                    shape: const RoundedRectangleBorder(
                      borderRadius: AqariRadius.mdBorder,
                    ),
                    leading: item.icon == null ? null : Icon(item.icon),
                    title: Text(item.label),
                    onTap: () => Navigator.pop(sheetContext, item.value),
                  ),
              ],
            ),
          ),
        ),
      );
      if (selected != null) onSelected(selected);
    },
  );
}
