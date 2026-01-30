using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using EssentialsPlus.Configuration;

namespace EssentialsPlus.Extensions
{
	public class PlayerInfo
	{
		private readonly List<Vector2> _backHistory = new List<Vector2>();
		private readonly List<string> _lastCommands = new List<string>();
		private CancellationTokenSource _timeCmd = new CancellationTokenSource();

        public const string KEY = "EssentialsPlus_Data";

		public IReadOnlyList<string> LastCommands => _lastCommands;
		public int BackHistoryCount => _backHistory.Count;
		public CancellationToken TimeCmdToken => _timeCmd.Token;

		public void CancelTimeCmd()
		{
			_timeCmd.Cancel();
			_timeCmd.Dispose();
			_timeCmd = new CancellationTokenSource();
		}

		public Vector2 PopBackHistory(int steps)
		{
			Vector2 vector = _backHistory[steps - 1];
			_backHistory.RemoveRange(0, steps);
			return vector;
		}
		public void PushBackHistory(Vector2 vector)
		{
			_backHistory.Insert(0, vector);
			if (_backHistory.Count > Config.Settings.BackPositionHistory)
			{
				_backHistory.RemoveAt(_backHistory.Count - 1);
			}
		}
        public void PushCommand(string command)
        {
			if (string.IsNullOrWhiteSpace(command))
			{
				return;
			}
            if (_lastCommands.Count > 0
				&& string.Equals(_lastCommands[0], command, StringComparison.OrdinalIgnoreCase))
            {
				return;
			}
            _lastCommands.Insert(0, command);
            if (_lastCommands.Count > Config.Settings.CommandHistory)
            {
				_lastCommands.RemoveAt(_lastCommands.Count - 1);
			}
        }
	}
}
