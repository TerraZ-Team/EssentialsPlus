using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using System.Linq;

namespace EssentialsPlus
{
	public class PlayerInfo
	{
		private List<Vector2> backHistory = new List<Vector2>();
		private CancellationTokenSource mute = new CancellationTokenSource();
		private CancellationTokenSource timeCmd = new CancellationTokenSource();
        public List<string> LastCommands = new List<string>();

        public const string KEY = "EssentialsPlus_Data";

		public int BackHistoryCount
		{
			get { return backHistory.Count; }
		}
		public CancellationToken MuteToken
		{
			get { return mute.Token; }
		}
		public CancellationToken TimeCmdToken
		{
			get { return timeCmd.Token; }
		}

		~PlayerInfo()
		{
			mute.Cancel();
			mute.Dispose();
			timeCmd.Cancel();
			timeCmd.Dispose();
		}

		public void CancelTimeCmd()
		{
			timeCmd.Cancel();
			timeCmd.Dispose();
			timeCmd = new CancellationTokenSource();
		}
		public Vector2 PopBackHistory(int steps)
		{
			Vector2 vector = backHistory[steps - 1];
			backHistory.RemoveRange(0, steps);
			return vector;
		}
		public void PushBackHistory(Vector2 vector)
		{
			backHistory.Insert(0, vector);
			if (backHistory.Count > EssentialsPlus.Config.BackPositionHistory)
			{
				backHistory.RemoveAt(backHistory.Count - 1);
			}
		}
        public void PushCommand(string command)
        { try {
            if (LastCommands.FirstOrDefault() == command)
            { return; }
            LastCommands.Insert(0, command);
            if (LastCommands.Count > EssentialsPlus.Config.CommandHistory)
            { LastCommands.RemoveAt(LastCommands.Count - 1); }
        } catch { } }
	}
}
