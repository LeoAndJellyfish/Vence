namespace Vence.Core.Commands;

public interface ICommand<out TResult>
{
    TResult Execute();
}
