using MediatR;

namespace PropertyOS.Application.Common.Interfaces;

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

public interface ICommand : ICommand<Unit>
{
}
