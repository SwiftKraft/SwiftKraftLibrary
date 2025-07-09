// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IGameServersService
    {
        Task<GameServerDeployResult> DeployAsync(GameServerDeployOptions deployOptions);
        Task<List<GameServerData>> ListAsync(GameServerListOptions listOptions);
        Task<OptionalGameServerData> MatchAsync(GameServerMatchOptions matchOptions);
        Task<GameServerData> GetAsync(ulong uniqueId);
        Task SuspendAsync(ulong uniqueId);
        Task ResumeAsync(ulong uniqueId);
        Task DeleteAsync(ulong uniqueId);
    }
}
