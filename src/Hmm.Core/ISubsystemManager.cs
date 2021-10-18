using Hmm.Core.DomainEntity;

namespace Hmm.Core
{
    public interface ISubsystemManager : IEntityManager<Subsystem>
    {
        bool Register(Subsystem subsystem);

        bool HasApplicationRegistered(Subsystem subsystem);
    }
}