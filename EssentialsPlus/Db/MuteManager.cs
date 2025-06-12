using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.DB;
using System.Linq;

namespace EssentialsPlus.Db
{
	public struct Mute
    {
		public Mute(int id)
        {
			ID = -1;

			accountId = null;
			authorId = null;

			ip = null;
			uuid = null;

			reason = String.Empty;

			date = DateTime.MinValue;
			expiration = DateTime.MinValue;
        }

		public int ID;

		public int? accountId;
		public int? authorId;

		public string? ip;
		public string? uuid;

		public string reason;

		public DateTime date;
		public DateTime expiration;
	}

	public class MuteManager
	{
		private IDbConnection db;

        public MuteManager(IDbConnection db)
		{
			this.db = db;

			var sqlCreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite
				? new SqliteQueryCreator()
				: new MysqlQueryCreator());

			sqlCreator.EnsureTableStructure(new SqlTable("Mutes",
				new SqlColumn("ID", MySqlDbType.Int32) { AutoIncrement = true, Primary = true },

				new SqlColumn("Account", MySqlDbType.Int32),
				new SqlColumn("Author", MySqlDbType.Int32),

				new SqlColumn("IP", MySqlDbType.Text),
				new SqlColumn("UUID", MySqlDbType.Text),

				new SqlColumn("Reason", MySqlDbType.Text),

				new SqlColumn("Date", MySqlDbType.Text),
				new SqlColumn("Expiration", MySqlDbType.Text)));
		}

		public bool Add(TSPlayer player, TSPlayer author, string reason, DateTime expiration, out Mute mute)
		{
			mute = new Mute(-1)
			{
				accountId = player.Account?.ID,
				authorId = author.Account.ID,

				ip = player.IP,
				uuid = player.UUID,

				reason = reason,
				expiration = expiration,

				date = DateTime.UtcNow
			};

			return Add(mute);
		}
		public bool Add(UserAccount account, TSPlayer author, string reason, DateTime expiration, out Mute mute)
        {
			mute = new Mute()
			{
				accountId = account.ID,
				authorId = author.Account.ID,

				ip = JsonConvert.DeserializeObject<List<string>>(account.KnownIps)?.LastOrDefault(),
				uuid = account.UUID,

				reason = reason,
				expiration = expiration,

				date = DateTime.UtcNow
			};

			return Add(mute);
		}
		public bool Add(Mute mute)
        {
			return db.Query("INSERT INTO Mutes VALUES(@0, @1, @2, @3, @4, @5, @6, @7)", null,
				mute.accountId, mute.authorId, mute.ip, mute.uuid, mute.reason, mute.date, mute.expiration) > 0;
		}

		public bool Remove(Mute mute)
        {
			if (mute.ID == -1)
				throw new ArgumentOutOfRangeException(nameof(mute.ID));

			return Remove(mute.ID);
        }
		public bool Remove(int id)
        {
			return db.Query("DELETE FROM Mutes WHERE ID = @0", id) > 0;
		}

		public Mute Read(IDataReader reader)
        {
			return new Mute()
			{
				ID = reader.GetInt32(0),

				accountId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
				authorId = reader.IsDBNull(2) ? null : reader.GetInt32(2),

				ip = reader.IsDBNull(3) ? null : reader.GetString(3),
				uuid = reader.IsDBNull(4) ? null : reader.GetString(4),

				reason = reader.GetString(5),
				date = DateTime.Parse(reader.GetString(6)),

				expiration = DateTime.Parse(reader.GetString(7))
			};
        }
		public Mute? GetMute(int id)
		{
			using (QueryResult result = db.QueryReader($"SELECT * FROM Mutes WHERE ID = @0", id))
            {
				if (result.Read())
					return Read(result.Reader);
            }
			return null;
		}

		public IEnumerable<Mute> GetMutes(TSPlayer player)
        {
			using (QueryResult result = db.QueryReader($"SELECT * FROM Mutes WHERE Account = @0 OR IP = @1 OR UUID = @2",
				player.Account?.ID ?? -1, player.IP, player.UUID))
			{
				while (result.Read())
				{
					yield return Read(result.Reader);
				}
			}
		}
		public IEnumerable<Mute> GetMutes(UserAccount account)
        {
			using (QueryResult result = db.QueryReader($"SELECT * FROM Mutes WHERE Account = @0 OR IP = @1 OR UUID = @2", 
				account.ID, JsonConvert.DeserializeObject<List<string>>(account.KnownIps)?.LastOrDefault(),
				account.UUID))
            {
				while (result.Read())
                {
					yield return Read(result.Reader);
                }
            }
		}
        public IEnumerable<(int ID, int Account, string Ip, string Reason, string Expiration)> GetActiveMutes()
        {
            using (QueryResult result = db.QueryReader("SELECT ID, Account, IP, Reason, Expiration FROM Mutes ORDER BY ID DESC"))
            {
                while (result.Read())
                {
                    int id = result.Get<int>("ID");
                    int account = result.Get<int>("Account");
					string ip = result.Get<string>("IP");
                    string reason = result.Get<string>("Reason");
                    string expiration = result.Get<string>("Expiration");

                    DateTime expirationDate;

                    bool isActive = !string.IsNullOrEmpty(expiration) && (!DateTime.TryParse(expiration, out expirationDate) || expirationDate > DateTime.UtcNow);

                    if (isActive)
                    {
                        yield return (id, account, ip, reason, expiration);//убрать айди, сделать возврат полного ника, по возможности сократить время окончания до секунд/минут
                    }
                }
            }
        }

    }
}