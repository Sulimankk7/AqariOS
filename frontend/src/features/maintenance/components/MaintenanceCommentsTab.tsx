import React, { useState } from 'react';
import { useMaintenanceComments, useAddMaintenanceComment } from '../hooks/useMaintenance';
import { Skeleton } from '@/app/components/ui/skeleton';
import { Button } from '@/app/components/ui/button';
import { MessageSquare } from 'lucide-react';
import { useTranslation } from '@/shared/i18n';
import { useMaintenanceActorName } from '../hooks/useMaintenanceActorName';

export const MaintenanceCommentsTab = ({ requestId }: { requestId: string }) => {
  const { t, formatDate } = useTranslation();
  const { data: comments, isLoading } = useMaintenanceComments(requestId);
  const { mutate: addComment, isPending } = useAddMaintenanceComment(requestId);
  const [newComment, setNewComment] = useState('');
  const actorName = useMaintenanceActorName();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.trim()) return;
    addComment({ commentText: newComment }, {
      onSuccess: () => setNewComment('')
    });
  };

  if (isLoading) {
    return (
      <div className="space-y-4 py-4">
        <Skeleton className="h-20 w-full" />
        <Skeleton className="h-20 w-full" />
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full space-y-4 pt-4">
      <div className="flex-1 overflow-y-auto space-y-4">
        {comments?.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground flex flex-col items-center">
            <MessageSquare className="w-8 h-8 mb-2 opacity-50" />
            <p>{t('maintenance.noComments')}</p>
          </div>
        ) : (
          comments?.map(comment => (
            <div key={comment.id} className="bg-secondary/20 p-3 rounded-lg border border-border">
              <div className="flex justify-between items-start mb-2">
                <span className="text-xs font-semibold text-foreground/80">
                  {actorName(comment.createdBy)}
                </span>
                <span className="text-xs text-muted-foreground" dir="ltr">
                  {formatDate(comment.createdAt)}
                </span>
              </div>
              <p className="text-sm whitespace-pre-wrap">{comment.commentText}</p>
            </div>
          ))
        )}
      </div>

      <form onSubmit={handleSubmit} className="mt-auto border-t border-border pt-4">
        <textarea
          className="w-full flex min-h-[80px] rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 mb-2"
          placeholder={t('maintenance.addComment')}
          value={newComment}
          onChange={(e) => setNewComment(e.target.value)}
          required
        />
        <div className="flex justify-end">
          <Button type="submit" disabled={!newComment.trim() || isPending}>
            {isPending ? t('maintenance.sending') : t('maintenance.sendComment')}
          </Button>
        </div>
      </form>
    </div>
  );
};
