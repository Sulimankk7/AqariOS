import 'dart:typed_data';
import 'dart:ui';
import 'package:file_selector/file_selector.dart';
import 'package:image_picker/image_picker.dart' as picker;
import 'package:share_plus/share_plus.dart';
import '../../../core/network/api_problem.dart';
import '../../../core/design_system/components/overlays/attachment_source_sheet.dart';
import '../../leasing/data/lease_files.dart';

class PaymentFiles {
  PaymentFiles(this.transfer, {picker.ImagePicker? imagePicker})
    : _imagePicker = imagePicker ?? picker.ImagePicker();
  final LeaseFiles transfer;
  final picker.ImagePicker _imagePicker;

  Future<XFile?> selectFile() => openFile(
    acceptedTypeGroups: [
      const XTypeGroup(
        label: 'Payment proof',
        extensions: ['pdf', 'png', 'jpg', 'jpeg'],
        mimeTypes: ['application/pdf', 'image/png', 'image/jpeg'],
      ),
    ],
  );

  Future<XFile?> selectCamera() =>
      _imagePicker.pickImage(source: picker.ImageSource.camera);

  Future<XFile?> selectGallery() =>
      _imagePicker.pickImage(source: picker.ImageSource.gallery);

  Future<XFile?> select(AttachmentSource source) => switch (source) {
    AttachmentSource.camera => selectCamera(),
    AttachmentSource.gallery => selectGallery(),
    AttachmentSource.files => selectFile(),
  };
  Future<void> validate(XFile file) async {
    final size = await file.length();
    if (size <= 0 || size > 15 * 1024 * 1024) {
      throw const ApiProblem(message: 'Invalid proof size', code: 'FILE_SIZE');
    }
    final mime =
        file.mimeType ??
        LeaseFiles.mimeTypes[file.name.split('.').last.toLowerCase()];
    if (![
      'application/pdf',
      'image/png',
      'image/jpeg',
      'image/jpg',
    ].contains(mime)) {
      throw const ApiProblem(message: 'Invalid proof type', code: 'FILE_TYPE');
    }
  }

  Future<String> upload(
    String paymentId,
    XFile file,
    void Function(String, double?) progress,
  ) async {
    await validate(file);
    return transfer.uploadFor('Financials', paymentId, file, progress);
  }

  Future<void> open(Uri uri) => transfer.open(uri);
  Future<void> sharePdf(Uint8List bytes, {required Rect origin}) async {
    await SharePlus.instance.share(
      ShareParams(
        files: [
          XFile.fromData(
            bytes,
            mimeType: 'application/pdf',
            name: 'Settlement.pdf',
          ),
        ],
        fileNameOverrides: ['Settlement.pdf'],
        sharePositionOrigin: origin,
      ),
    );
  }
}
