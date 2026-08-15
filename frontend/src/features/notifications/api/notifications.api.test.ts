import test from 'node:test';
import assert from 'node:assert/strict';
import { notificationsApi } from './notifications.api';
import { http } from '@/shared/lib/http';

test('notificationsApi.markAllAsRead sends a single PATCH request to /api/v1/notifications/me/read-all', async () => {
  let calledUrl = '';
  let calledMethod = '';

  const originalPatch = http.patch;
  try {
    http.patch = (async (url: string) => {
      calledUrl = url;
      calledMethod = 'PATCH';
      return { markedCount: 5 };
    }) as any;

    const result = await notificationsApi.markAllAsRead();

    assert.equal(calledMethod, 'PATCH');
    assert.equal(calledUrl, '/api/v1/notifications/me/read-all');
    assert.equal(result.markedCount, 5);
  } finally {
    http.patch = originalPatch;
  }
});

test('notificationsApi.markAsRead sends single notification PATCH to /api/v1/notifications/:id/read', async () => {
  let calledUrl = '';
  const testId = '01900000-0000-7000-8000-000000000001';

  const originalPatch = http.patch;
  try {
    http.patch = (async (url: string) => {
      calledUrl = url;
      return undefined;
    }) as any;

    await notificationsApi.markAsRead(testId);

    assert.equal(calledUrl, `/api/v1/notifications/${testId}/read`);
  } finally {
    http.patch = originalPatch;
  }
});

test('notificationsApi.getUnreadCount sends GET request to /api/v1/notifications/me/unread-count', async () => {
  let calledUrl = '';

  const originalGet = http.get;
  try {
    http.get = (async (url: string) => {
      calledUrl = url;
      return 3;
    }) as any;

    const count = await notificationsApi.getUnreadCount();

    assert.equal(calledUrl, '/api/v1/notifications/me/unread-count');
    assert.equal(count, 3);
  } finally {
    http.get = originalGet;
  }
});
