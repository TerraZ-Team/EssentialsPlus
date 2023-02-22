using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.DB;
using Auxiliary;
using System.Linq;
using MongoDB.Driver;

namespace EssentialsPlus.Db
{
	public class Mute : BsonModel
	{
		string _violator = String.Empty;
		public string Violator
		{
			get => _violator;
			set { _ = this.SaveAsync(x => x.Violator, value); _violator = value; }
		}

		string _uuid = String.Empty;
		public string UUID
		{
			get => _uuid;
			set { _ = this.SaveAsync(x => x.UUID, value); _uuid = value; }
		}

		string _ip = String.Empty;
		public string IP
		{
			get => _ip;
			set { _ = this.SaveAsync(x => x.IP, value); _ip = value; }
		}

		string _reason = String.Empty;
		public string Reason
		{
			get => _reason;
			set { _ = this.SaveAsync(x => x.Reason, value); _reason = value; }
		}

		string _author = String.Empty;
		public string Author
		{
			get => _author;
			set { _ = this.SaveAsync(x => x.Author, value); _author = value; }
		}

		DateTime _date = DateTime.MinValue;
		public DateTime Date
		{
			get => _date;
			set { _ = this.SaveAsync(x => x.Date, value); _date = value; }
		}

		DateTime _expiration = DateTime.MinValue;
		public DateTime Expiration
		{
			get => _expiration;
			set { _ = this.SaveAsync(x => x.Expiration, value); _expiration = value; }
		}
	}

	public class MuteManager
	{
		public async Task<Mute> AddAsync(TSPlayer player, string reason, DateTime expiration, string? author = null)
        {
			UserAccount account = player.Account ?? new UserAccount() { 
				KnownIps = JsonConvert.SerializeObject(new List<string> { player.IP }, Formatting.Indented), UUID = player.UUID };
			return await AddAsync(account, reason, expiration, author);
		}
		public async Task<Mute> AddAsync(UserAccount account, string reason, DateTime expiration, string? author = null)
        {
			if (account == null || string.IsNullOrEmpty(account.KnownIps) || string.IsNullOrEmpty(account.UUID))
				throw new NullReferenceException("account");
			return await IModel.CreateAsync(CreateRequest.Bson<Mute>(x =>
			{
				x.Violator = account.Name ?? "";

				x.IP = JsonConvert.DeserializeObject<List<string>>(account.KnownIps).Last();
				x.UUID = account.UUID;

				x.Reason = reason;

				x.Author = author ?? "";

				x.Date = DateTime.UtcNow;
				x.Expiration = expiration;
			}));
		}

		public async Task<List<Mute>> GetUserMuteAsync(TSPlayer player)
		{
			string plyName = player.IsLoggedIn ? player.Account.Name : player.Name;
			await IModel.GetAsync(GetRequest.Bson<Mute>(x =>
				x.Violator == plyName || x.IP == player.IP || x.UUID == player.UUID));
			List<Mute> mutes = StorageProvider.GetMongoCollection<Mute>("Mutes").Find(x => 
				x.Violator == plyName && x.IP == player.IP || x.UUID == player.UUID).Limit(2).ToList();
			return mutes;
		}
		public async Task<List<Mute>> GetUserMuteAsync(UserAccount account)
		{
			if (account == null || string.IsNullOrEmpty(account.KnownIps) || string.IsNullOrEmpty(account.UUID))
				throw new NullReferenceException("account");
			string ip = JsonConvert.DeserializeObject<List<string>>(account.KnownIps).Last();
			await IModel.GetAsync(GetRequest.Bson<Mute>(x =>
				x.Violator == account.Name || x.IP == ip || x.UUID == account.UUID));
			List<Mute> mutes = StorageProvider.GetMongoCollection<Mute>("Mutes").Find(x =>
				x.Violator == account.Name || x.IP == ip || x.UUID == account.UUID).Limit(2).ToList();
			return mutes;
		}

		public async Task<bool> ArchiveUserMuteAsync(TSPlayer player)
		{
			var mutes = await GetUserMuteAsync(player);
			mutes.ForEach(mute => mute.Expiration = DateTime.MinValue);
			if (mutes.Count > 0)
				return true;
			return false;
		}
		public async Task<bool> ArchiveUserMuteAsync(UserAccount account)
		{
			var mutes = await GetUserMuteAsync(account);
			mutes.ForEach(mute => mute.Expiration = DateTime.MinValue);
			if (mutes.Count > 0)
				return true;
			return false;
		}
	}
}
