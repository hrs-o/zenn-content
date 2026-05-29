using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace UnityTower.Main
{
    public class MainLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            RegisterComponent(builder);
            RegisterMessagePipe(builder);
        }

        private void RegisterComponent(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<GameManager>();
            builder.RegisterComponentInHierarchy<Block>();
        }

        private void RegisterMessagePipe(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();

            builder.RegisterRequestHandler<BlockGenerateRequest, Block, BlockGenerateHandler>(options);
        }
    }
}
