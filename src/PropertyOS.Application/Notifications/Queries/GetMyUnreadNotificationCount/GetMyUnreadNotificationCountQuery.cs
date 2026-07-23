using MediatR;

namespace PropertyOS.Application.Notifications.Queries.GetMyUnreadNotificationCount;

public record GetMyUnreadNotificationCountQuery : IRequest<int>;
