class InboxNotification {
  const InboxNotification({
    required this.id,
    required this.subject,
    required this.body,
    required this.type,
    required this.priority,
    required this.status,
    required this.createdAt,
    required this.readAt,
  });

  final String id, subject, body, createdAt;
  final int type, priority, status;
  final String? readAt;

  factory InboxNotification.fromJson(Map<String, dynamic> json) =>
      InboxNotification(
        id: json['id']?.toString() ?? '',
        subject: json['subject']?.toString() ?? '',
        body: json['body']?.toString() ?? '',
        type: (json['notificationType'] as num?)?.toInt() ?? 9,
        priority: (json['priority'] as num?)?.toInt() ?? 1,
        status: (json['status'] as num?)?.toInt() ?? 0,
        createdAt: json['createdAt']?.toString() ?? '',
        readAt: json['readAt']?.toString(),
      );

  bool get canMarkRead => status == 1 && readAt == null;
  InboxNotification get markedRead => InboxNotification(
    id: id,
    subject: subject,
    body: body,
    type: type,
    priority: priority,
    status: status,
    createdAt: createdAt,
    readAt: DateTime.now().toUtc().toIso8601String(),
  );
}

class NotificationPageCursor {
  const NotificationPageCursor({this.createdAt, this.id});
  final String? createdAt, id;
  Map<String, String> get query => {
    'pageSize': '50',
    if (createdAt != null && id != null) 'lastSeenCreatedAt': createdAt!,
    if (createdAt != null && id != null) 'lastSeenId': id!,
  };
}
