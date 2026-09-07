import 'package:flutter/material.dart';

import '../../design_system.dart';

enum AttachmentSource { camera, gallery, files }

class AttachmentSourceSheet {
  const AttachmentSourceSheet._();

  static Future<AttachmentSource?> show(BuildContext context) =>
      showModalBottomSheet<AttachmentSource>(
        context: context,
        useSafeArea: true,
        showDragHandle: true,
        builder: (context) => Padding(
          padding: const EdgeInsets.fromLTRB(
            AqariSpacing.page,
            0,
            AqariSpacing.page,
            AqariSpacing.x6,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                context.isArabic ? 'إضافة مرفق' : 'Add attachment',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: AqariSpacing.x3),
              _choice(
                context,
                AttachmentSource.camera,
                Icons.photo_camera_outlined,
                context.isArabic ? 'الكاميرا' : 'Camera',
              ),
              _choice(
                context,
                AttachmentSource.gallery,
                Icons.photo_library_outlined,
                context.isArabic ? 'معرض الصور' : 'Gallery',
              ),
              _choice(
                context,
                AttachmentSource.files,
                Icons.folder_open_outlined,
                context.isArabic ? 'الملفات' : 'Files',
              ),
            ],
          ),
        ),
      );

  static Widget _choice(
    BuildContext context,
    AttachmentSource value,
    IconData icon,
    String label,
  ) => ListTile(
    leading: Icon(icon, color: context.aqariColors.primary),
    title: Text(label),
    shape: const RoundedRectangleBorder(borderRadius: AqariRadius.mdBorder),
    onTap: () => Navigator.pop(context, value),
  );
}
