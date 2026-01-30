using System;
using Terraria;
using TerrariaApi.Server;
using EssentialsPlus.Handlers;

namespace EssentialsPlus
{
	[ApiVersion(2, 1)]
	public class EssentialsPlus : TerrariaPlugin
	{
		public override string Author => "Xekep";
		public override string Description => "Essentials, but better";
		public override string Name => "EssentialsPlus";
		public override Version Version => new(2, 0);
		private TShockEventsHandler _eventsHandler;

		public EssentialsPlus(Main game) : base(game)
		{
			Order = int.MaxValue;
			_eventsHandler = new(this);
        }

        public override void Initialize()
        {
            _eventsHandler.RegisterHandlers();
            TshockCommandsHandler.RegisterCommands();
        }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_eventsHandler.UnregisterHandlers();
				TshockCommandsHandler.UnregisterCommands();
			}
			base.Dispose(disposing);
		}
	}
}