using System.IO;
using Newtonsoft.Json;

namespace EssentialsPlus
{
	public class Settings
	{
		public string[] DisabledCommandsInPvp = new string[]
		{
			"back"
		};

		public int BackPositionHistory = 10;
        public int CommandHistory = 10;
	}
}
