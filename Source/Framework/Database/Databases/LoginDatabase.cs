﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Database;

public class LoginDatabase : MySqlBase<LoginStatements>
{
	public override void PreparedStatements()
	{
		const string BnetAccountInfo = "ba.id, ba.email, ba.locked, ba.lock_country, ba.last_ip, ba.LoginTicketExpiry, bab.unbandate > UNIX_TIMESTAMP() OR bab.unbandate = bab.bandate, bab.unbandate = bab.bandate";
		const string BnetGameAccountInfo = "a.id, a.username, ab.unbandate, ab.unbandate = ab.bandate, aa.SecurityLevel";

		PrepareStatement(LoginStatements.SEL_REALMLIST, "SELECT id, name, address, localAddress, localSubnetMask, port, icon, flag, timezone, allowedSecurityLevel, population, gamebuild, Region, Battlegroup FROM realmlist WHERE flag <> 3 ORDER BY name");
		PrepareStatement(LoginStatements.DelExpiredIpBans, "DELETE FROM ip_banned WHERE unbandate<>bandate AND unbandate<=UNIX_TIMESTAMP()");
		PrepareStatement(LoginStatements.UpdExpiredAccountBans, "UPDATE account_banned SET active = 0 WHERE active = 1 AND unbandate<>bandate AND unbandate<=UNIX_TIMESTAMP()");
		PrepareStatement(LoginStatements.SelIpInfo, "SELECT unbandate > UNIX_TIMESTAMP() OR unbandate = bandate AS banned, NULL as country FROM ip_banned WHERE ip = ?");

		PrepareStatement(LoginStatements.InsIpAutoBanned, "INSERT INTO ip_banned (ip, bandate, unbandate, bannedby, banreason) VALUES (?, UNIX_TIMESTAMP(), UNIX_TIMESTAMP()+?, 'Cypher Auth', 'Failed login autoban')");
		PrepareStatement(LoginStatements.SEL_IP_BANNED_ALL, "SELECT ip, bandate, unbandate, bannedby, banreason FROM ip_banned WHERE (bandate = unbandate OR unbandate > UNIX_TIMESTAMP()) ORDER BY unbandate");
		PrepareStatement(LoginStatements.SEL_IP_BANNED_BY_IP, "SELECT ip, bandate, unbandate, bannedby, banreason FROM ip_banned WHERE (bandate = unbandate OR unbandate > UNIX_TIMESTAMP()) AND ip LIKE CONCAT('%%', ?, '%%') ORDER BY unbandate");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_BANNED_ALL, "SELECT account.id, username FROM account, account_banned WHERE account.id = account_banned.id AND active = 1 GROUP BY account.id");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_BANNED_BY_FILTER, "SELECT account.id, username FROM account, account_banned WHERE account.id = account_banned.id AND active = 1 AND username LIKE CONCAT('%%', ?, '%%') GROUP BY account.id");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_BANNED_BY_USERNAME, "SELECT account.id, username FROM account, account_banned WHERE account.id = account_banned.id AND active = 1 AND username = ? GROUP BY account.id");
		PrepareStatement(LoginStatements.DEL_ACCOUNT_BANNED, "DELETE FROM account_banned WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_ACCOUNT_INFO_CONTINUED_SESSION, "UPDATE account SET session_key_bnet = ? WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_INFO_CONTINUED_SESSION, "SELECT username, session_key_bnet FROM account WHERE id = ? AND LENGTH(session_key_bnet) = 40");
		PrepareStatement(LoginStatements.UPD_LOGON, "UPDATE account SET salt = ?, verifier = ? WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_ID_BY_NAME, "SELECT id FROM account WHERE username = ?");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_LIST_BY_NAME, "SELECT id, username FROM account WHERE username = ?");

		PrepareStatement(LoginStatements.SEL_ACCOUNT_INFO_BY_NAME,
						"SELECT a.id, a.session_key_bnet, ba.last_ip, ba.locked, ba.lock_country, a.expansion, a.mutetime, ba.locale, a.recruiter, a.os, ba.id, aa.SecurityLevel, " +
						"bab.unbandate > UNIX_TIMESTAMP() OR bab.unbandate = bab.bandate, ab.unbandate > UNIX_TIMESTAMP() OR ab.unbandate = ab.bandate, r.id " +
						"FROM account a LEFT JOIN account r ON a.id = r.recruiter LEFT JOIN battlenet_accounts ba ON a.battlenet_account = ba.id " +
						"LEFT JOIN account_access aa ON a.id = aa.AccountID AND aa.RealmID IN (-1, ?) LEFT JOIN battlenet_account_bans bab ON ba.id = bab.id LEFT JOIN account_banned ab ON a.id = ab.id AND ab.active = 1 " +
						"WHERE a.username = ? AND LENGTH(a.session_key_bnet) = 64 ORDER BY aa.RealmID DESC LIMIT 1");

		PrepareStatement(LoginStatements.SEL_ACCOUNT_LIST_BY_EMAIL, "SELECT id, username FROM account WHERE email = ?");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_BY_IP, "SELECT id, username FROM account WHERE last_ip = ?");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_BY_ID, "SELECT 1 FROM account WHERE id = ?");
		PrepareStatement(LoginStatements.INS_IP_BANNED, "INSERT INTO ip_banned (ip, bandate, unbandate, bannedby, banreason) VALUES (?, UNIX_TIMESTAMP(), UNIX_TIMESTAMP()+?, ?, ?)");
		PrepareStatement(LoginStatements.DEL_IP_NOT_BANNED, "DELETE FROM ip_banned WHERE ip = ?");
		PrepareStatement(LoginStatements.INS_ACCOUNT_BANNED, "INSERT INTO account_banned (id, bandate, unbandate, bannedby, banreason, active) VALUES (?, UNIX_TIMESTAMP(), UNIX_TIMESTAMP()+?, ?, ?, 1)");
		PrepareStatement(LoginStatements.UPD_ACCOUNT_NOT_BANNED, "UPDATE account_banned SET active = 0 WHERE id = ? AND active != 0");
		PrepareStatement(LoginStatements.DEL_REALM_CHARACTERS, "DELETE FROM realmcharacters WHERE acctid = ?");
		PrepareStatement(LoginStatements.REP_REALM_CHARACTERS, "REPLACE INTO realmcharacters (numchars, acctid, realmid) VALUES (?, ?, ?)");
		PrepareStatement(LoginStatements.SEL_SUM_REALM_CHARACTERS, "SELECT SUM(numchars) FROM realmcharacters WHERE acctid = ?");
		PrepareStatement(LoginStatements.INS_ACCOUNT, "INSERT INTO account (username, salt, verifier, reg_mail, email, joindate, battlenet_account, battlenet_index) VALUES(?, ?, ?, ?, ?, NOW(), ?, ?)");
		PrepareStatement(LoginStatements.INS_REALM_CHARACTERS_INIT, "INSERT INTO realmcharacters (realmid, acctid, numchars) SELECT realmlist.id, account.id, 0 FROM realmlist, account LEFT JOIN realmcharacters ON acctid = account.id WHERE acctid IS NULL");
		PrepareStatement(LoginStatements.UPD_EXPANSION, "UPDATE account SET expansion = ? WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_ACCOUNT_LOCK, "UPDATE account SET locked = ? WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_ACCOUNT_LOCK_COUNTRY, "UPDATE account SET lock_country = ? WHERE id = ?");
		PrepareStatement(LoginStatements.INS_LOG, "INSERT INTO logs (time, realm, type, level, string) VALUES (?, ?, ?, ?, ?)");
		PrepareStatement(LoginStatements.UPD_USERNAME, "UPDATE account SET username = ? WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_EMAIL, "UPDATE account SET email = ? WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_REG_EMAIL, "UPDATE account SET reg_mail = ? WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_MUTE_TIME, "UPDATE account SET mutetime = ? , mutereason = ? , muteby = ? WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_MUTE_TIME_LOGIN, "UPDATE account SET mutetime = ? WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_LAST_IP, "UPDATE account SET last_ip = ? WHERE username = ?");
		PrepareStatement(LoginStatements.UPD_LAST_ATTEMPT_IP, "UPDATE account SET last_attempt_ip = ? WHERE username = ?");
		PrepareStatement(LoginStatements.UPD_ACCOUNT_ONLINE, "UPDATE account SET online = 1 WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_UPTIME_PLAYERS, "UPDATE uptime SET uptime = ?, maxplayers = ? WHERE realmid = ? AND starttime = ?");
		PrepareStatement(LoginStatements.DEL_OLD_LOGS, "DELETE FROM logs WHERE (time + ?) < ? AND realm = ?");
		PrepareStatement(LoginStatements.DEL_ACCOUNT_ACCESS, "DELETE FROM account_access WHERE AccountID = ?");
		PrepareStatement(LoginStatements.DEL_ACCOUNT_ACCESS_BY_REALM, "DELETE FROM account_access WHERE AccountID = ? AND (RealmID = ? OR RealmID = -1)");
		PrepareStatement(LoginStatements.INS_ACCOUNT_ACCESS, "INSERT INTO account_access (AccountID, SecurityLevel, RealmID) VALUES (?, ?, ?)");
		PrepareStatement(LoginStatements.GET_ACCOUNT_ID_BY_USERNAME, "SELECT id FROM account WHERE username = ?");
		PrepareStatement(LoginStatements.GET_GMLEVEL_BY_REALMID, "SELECT SecurityLevel FROM account_access WHERE AccountID = ? AND (RealmID = ? OR RealmID = -1) ORDER BY RealmID DESC");
		PrepareStatement(LoginStatements.GET_USERNAME_BY_ID, "SELECT username FROM account WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_CHECK_PASSWORD, "SELECT salt, verifier FROM account WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_CHECK_PASSWORD_BY_NAME, "SELECT salt, verifier FROM account WHERE username = ?");
		PrepareStatement(LoginStatements.SEL_PINFO, "SELECT a.username, aa.SecurityLevel, a.email, a.reg_mail, a.last_ip, DATE_FORMAT(a.last_login, '%Y-%m-%d %T'), a.mutetime, a.mutereason, a.muteby, a.failed_logins, a.locked, a.OS FROM account a LEFT JOIN account_access aa ON (a.id = aa.AccountID AND (aa.RealmID = ? OR aa.RealmID = -1)) WHERE a.id = ?");
		PrepareStatement(LoginStatements.SEL_PINFO_BANS, "SELECT unbandate, bandate = unbandate, bannedby, banreason FROM account_banned WHERE id = ? AND active ORDER BY bandate ASC LIMIT 1");
		PrepareStatement(LoginStatements.SEL_GM_ACCOUNTS, "SELECT a.username, aa.SecurityLevel FROM account a, account_access aa WHERE a.id = aa.AccountID AND aa.SecurityLevel >= ? AND (aa.RealmID = -1 OR aa.RealmID = ?)");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_INFO, "SELECT a.username, a.last_ip, aa.SecurityLevel, a.expansion FROM account a LEFT JOIN account_access aa ON a.id = aa.AccountID WHERE a.id = ?");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_ACCESS_SECLEVEL_TEST, "SELECT 1 FROM account_access WHERE AccountID = ? AND SecurityLevel > ?");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_ACCESS, "SELECT a.id, aa.SecurityLevel, aa.RealmID FROM account a LEFT JOIN account_access aa ON a.id = aa.AccountID WHERE a.username = ?");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_WHOIS, "SELECT username, email, last_ip FROM account WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_LAST_ATTEMPT_IP, "SELECT last_attempt_ip FROM account WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_LAST_IP, "SELECT last_ip FROM account WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_REALMLIST_SECURITY_LEVEL, "SELECT allowedSecurityLevel from realmlist WHERE id = ?");
		PrepareStatement(LoginStatements.DEL_ACCOUNT, "DELETE FROM account WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_AUTOBROADCAST, "SELECT id, weight, text FROM autobroadcast WHERE realmid = ? OR realmid = -1");
		PrepareStatement(LoginStatements.GET_EMAIL_BY_ID, "SELECT email FROM account WHERE id = ?");
		// 0: uint32, 1: uint32, 2: uint8, 3: uint32, 4: string // Complete name: "Login_Insert_AccountLoginDeLete_IP_Logging"
		PrepareStatement(LoginStatements.INS_ALDL_IP_LOGGING, "INSERT INTO logs_ip_actions (account_id,character_guid,type,ip,systemnote,unixtime,time) VALUES (?, ?, ?, (SELECT last_ip FROM account WHERE id = ?), ?, unix_timestamp(NOW()), NOW())");
		// 0: uint32, 1: uint32, 2: uint8, 3: uint32, 4: string // Complete name: "Login_Insert_FailedAccountLogin_IP_Logging"
		PrepareStatement(LoginStatements.INS_FACL_IP_LOGGING, "INSERT INTO logs_ip_actions (account_id,character_guid,type,ip,systemnote,unixtime,time) VALUES (?, ?, ?, (SELECT last_attempt_ip FROM account WHERE id = ?), ?, unix_timestamp(NOW()), NOW())");
		// 0: uint32, 1: uint32, 2: uint8, 3: string, 4: string // Complete name: "Login_Insert_CharacterDelete_IP_Logging"
		PrepareStatement(LoginStatements.INS_CHAR_IP_LOGGING, "INSERT INTO logs_ip_actions (account_id,character_guid,type,ip,systemnote,unixtime,time) VALUES (?, ?, ?, ?, ?, unix_timestamp(NOW()), NOW())");
		// 0: string, 1: string, 2: string                      // Complete name: "Login_Insert_Failed_Account_Login_due_password_IP_Logging"
		PrepareStatement(LoginStatements.INS_FALP_IP_LOGGING, "INSERT INTO logs_ip_actions (account_id,character_guid,type,ip,systemnote,unixtime,time) VALUES (?, 0, 1, ?, ?, unix_timestamp(NOW()), NOW())");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_ACCESS_BY_ID, "SELECT SecurityLevel, RealmID FROM account_access WHERE AccountID = ? and (RealmID = ? OR RealmID = -1) ORDER BY SecurityLevel desc");

		PrepareStatement(LoginStatements.SEL_RBAC_ACCOUNT_PERMISSIONS, "SELECT permissionId, granted FROM rbac_account_permissions WHERE accountId = ? AND (realmId = ? OR realmId = -1) ORDER BY permissionId, realmId");
		PrepareStatement(LoginStatements.INS_RBAC_ACCOUNT_PERMISSION, "INSERT INTO rbac_account_permissions (accountId, permissionId, granted, realmId) VALUES (?, ?, ?, ?) ON DUPLICATE KEY UPDATE granted = VALUES(granted)");
		PrepareStatement(LoginStatements.DEL_RBAC_ACCOUNT_PERMISSION, "DELETE FROM rbac_account_permissions WHERE accountId = ? AND permissionId = ? AND (realmId = ? OR realmId = -1)");

		PrepareStatement(LoginStatements.INS_ACCOUNT_MUTE, "INSERT INTO account_muted VALUES (?, UNIX_TIMESTAMP(), ?, ?, ?)");
		PrepareStatement(LoginStatements.SEL_ACCOUNT_MUTE_INFO, "SELECT mutedate, mutetime, mutereason, mutedby FROM account_muted WHERE guid = ? ORDER BY mutedate ASC");
		PrepareStatement(LoginStatements.DEL_ACCOUNT_MUTED, "DELETE FROM account_muted WHERE guid = ?");

		PrepareStatement(LoginStatements.SelBnetAuthentication, "SELECT ba.id, ba.sha_pass_hash, ba.failed_logins, ba.LoginTicket, ba.LoginTicketExpiry, bab.unbandate > UNIX_TIMESTAMP() OR bab.unbandate = bab.bandate FROM battlenet_accounts ba LEFT JOIN battlenet_account_bans bab ON ba.id = bab.id WHERE email = ?");
		PrepareStatement(LoginStatements.UpdBnetAuthentication, "UPDATE battlenet_accounts SET LoginTicket = ?, LoginTicketExpiry = ? WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_BNET_EXISTING_AUTHENTICATION_BY_ID, "SELECT LoginTicket FROM battlenet_accounts WHERE id = ?");

		PrepareStatement(LoginStatements.SelBnetAccountInfo,
						"SELECT " +
						BnetAccountInfo +
						", " +
						BnetGameAccountInfo +
						" FROM battlenet_accounts ba LEFT JOIN battlenet_account_bans bab ON ba.id = bab.id LEFT JOIN account a ON ba.id = a.battlenet_account" +
						" LEFT JOIN account_banned ab ON a.id = ab.id AND ab.active = 1 LEFT JOIN account_access aa ON a.id = aa.AccountID AND aa.RealmID = -1 WHERE ba.LoginTicket = ? ORDER BY a.id");

		PrepareStatement(LoginStatements.UpdBnetLastLoginInfo, "UPDATE battlenet_accounts SET last_ip = ?, last_login = NOW(), locale = ?, failed_logins = 0, os = ? WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LOGIN_INFO, "UPDATE account SET session_key_bnet = ?, last_ip = ?, last_login = NOW(), locale = ?, failed_logins = 0, os = ? WHERE username = ?");
		PrepareStatement(LoginStatements.SelBnetCharacterCountsByAccountId, "SELECT rc.acctid, rc.numchars, r.id, r.Region, r.Battlegroup FROM realmcharacters rc INNER JOIN realmlist r ON rc.realmid = r.id WHERE rc.acctid = ?");
		PrepareStatement(LoginStatements.SEL_BNET_CHARACTER_COUNTS_BY_BNET_ID, "SELECT rc.acctid, rc.numchars, r.id, r.Region, r.Battlegroup FROM realmcharacters rc INNER JOIN realmlist r ON rc.realmid = r.id LEFT JOIN account a ON rc.acctid = a.id WHERE a.battlenet_account = ?");
		PrepareStatement(LoginStatements.SelBnetLastPlayerCharacters, "SELECT lpc.accountId, lpc.region, lpc.battlegroup, lpc.realmId, lpc.characterName, lpc.characterGUID, lpc.lastPlayedTime FROM account_last_played_character lpc LEFT JOIN account a ON lpc.accountId = a.id WHERE a.battlenet_account = ?");
		PrepareStatement(LoginStatements.DEL_BNET_LAST_PLAYER_CHARACTERS, "DELETE FROM account_last_played_character WHERE accountId = ? AND region = ? AND battlegroup = ?");
		PrepareStatement(LoginStatements.INS_BNET_LAST_PLAYER_CHARACTERS, "INSERT INTO account_last_played_character (accountId, region, battlegroup, realmId, characterName, characterGUID, lastPlayedTime) VALUES (?,?,?,?,?,?,?)");
		PrepareStatement(LoginStatements.INS_BNET_ACCOUNT, "INSERT INTO battlenet_accounts (`email`,`sha_pass_hash`) VALUES (?, ?)");
		PrepareStatement(LoginStatements.SEL_BNET_ACCOUNT_EMAIL_BY_ID, "SELECT email FROM battlenet_accounts WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_BNET_ACCOUNT_ID_BY_EMAIL, "SELECT id FROM battlenet_accounts WHERE email = ?");
		PrepareStatement(LoginStatements.UPD_BNET_PASSWORD, "UPDATE battlenet_accounts SET sha_pass_hash = ? WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_BNET_CHECK_PASSWORD, "SELECT 1 FROM battlenet_accounts WHERE id = ? AND sha_pass_hash = ?");
		PrepareStatement(LoginStatements.UPD_BNET_ACCOUNT_LOCK, "UPDATE battlenet_accounts SET locked = ? WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_BNET_ACCOUNT_LOCK_CONTRY, "UPDATE battlenet_accounts SET lock_country = ? WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_BNET_ACCOUNT_ID_BY_GAME_ACCOUNT, "SELECT battlenet_account FROM account WHERE id = ?");
		PrepareStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LINK, "UPDATE account SET battlenet_account = ?, battlenet_index = ? WHERE id = ?");
		PrepareStatement(LoginStatements.SEL_BNET_MAX_ACCOUNT_INDEX, "SELECT MAX(battlenet_index) FROM account WHERE battlenet_account = ?");
		PrepareStatement(LoginStatements.SEL_BNET_GAME_ACCOUNT_LIST, "SELECT a.id, a.username FROM account a LEFT JOIN battlenet_accounts ba ON a.battlenet_account = ba.id WHERE ba.email = ?");

		PrepareStatement(LoginStatements.UpdBnetFailedLogins, "UPDATE battlenet_accounts SET failed_logins = failed_logins + 1 WHERE id = ?");
		PrepareStatement(LoginStatements.InsBnetAccountAutoBanned, "INSERT INTO battlenet_account_bans(id, bandate, unbandate, bannedby, banreason) VALUES(?, UNIX_TIMESTAMP(), UNIX_TIMESTAMP()+?, 'Trinity Auth', 'Failed login autoban')");
		PrepareStatement(LoginStatements.DelBnetExpiredAccountBanned, "DELETE FROM battlenet_account_bans WHERE unbandate<>bandate AND unbandate<=UNIX_TIMESTAMP()");
		PrepareStatement(LoginStatements.UpdBnetResetFailedLogins, "UPDATE battlenet_accounts SET failed_logins = 0 WHERE id = ?");

		PrepareStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE, "SELECT LastCharacterUndelete FROM battlenet_accounts WHERE Id = ?");
		PrepareStatement(LoginStatements.UPD_LAST_CHAR_UNDELETE, "UPDATE battlenet_accounts SET LastCharacterUndelete = UNIX_TIMESTAMP() WHERE Id = ?");

		// Account wide toys
		PrepareStatement(LoginStatements.SEL_ACCOUNT_TOYS, "SELECT itemId, isFavourite, hasFanfare FROM battlenet_account_toys WHERE accountId = ?");
		PrepareStatement(LoginStatements.REP_ACCOUNT_TOYS, "REPLACE INTO battlenet_account_toys (accountId, itemId, isFavourite, hasFanfare) VALUES (?, ?, ?, ?)");

		// Battle Pets
		PrepareStatement(LoginStatements.SEL_BATTLE_PETS, "SELECT bp.guid, bp.species, bp.breed, bp.displayId, bp.level, bp.exp, bp.health, bp.quality, bp.flags, bp.name, bp.nameTimestamp, bp.owner, dn.genitive, dn.dative, dn.accusative, dn.instrumental, dn.prepositional FROM battle_pets bp LEFT JOIN battle_pet_declinedname dn ON bp.guid = dn.guid WHERE bp.battlenetAccountId = ? AND (bp.ownerRealmId IS NULL OR bp.ownerRealmId = ?)");
		PrepareStatement(LoginStatements.INS_BATTLE_PETS, "INSERT INTO battle_pets (guid, battlenetAccountId, species, breed, displayId, level, exp, health, quality, flags, name, nameTimestamp, owner, ownerRealmId) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
		PrepareStatement(LoginStatements.DEL_BATTLE_PETS, "DELETE FROM battle_pets WHERE battlenetAccountId = ? AND guid = ?");
		PrepareStatement(LoginStatements.DEL_BATTLE_PETS_BY_OWNER, "DELETE FROM battle_pets WHERE owner = ? AND ownerRealmId = ?");
		PrepareStatement(LoginStatements.UPD_BATTLE_PETS, "UPDATE battle_pets SET level = ?, exp = ?, health = ?, quality = ?, flags = ?, name = ?, nameTimestamp = ? WHERE battlenetAccountId = ? AND guid = ?");
		PrepareStatement(LoginStatements.SEL_BATTLE_PET_SLOTS, "SELECT id, battlePetGuid, locked FROM battle_pet_slots WHERE battlenetAccountId = ?");
		PrepareStatement(LoginStatements.INS_BATTLE_PET_SLOTS, "INSERT INTO battle_pet_slots (id, battlenetAccountId, battlePetGuid, locked) VALUES (?, ?, ?, ?)");
		PrepareStatement(LoginStatements.DEL_BATTLE_PET_SLOTS, "DELETE FROM battle_pet_slots WHERE battlenetAccountId = ?");
		PrepareStatement(LoginStatements.INS_BATTLE_PET_DECLINED_NAME, "INSERT INTO battle_pet_declinedname (guid, genitive, dative, accusative, instrumental, prepositional) VALUES (?, ?, ?, ?, ?, ?)");
		PrepareStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME, "DELETE FROM battle_pet_declinedname WHERE guid = ?");
		PrepareStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME_BY_OWNER, "DELETE dn FROM battle_pet_declinedname dn INNER JOIN battle_pets bp ON dn.guid = bp.guid WHERE bp.owner = ? AND bp.ownerRealmId = ?");

		PrepareStatement(LoginStatements.SEL_ACCOUNT_HEIRLOOMS, "SELECT itemId, flags FROM battlenet_account_heirlooms WHERE accountId = ?");
		PrepareStatement(LoginStatements.REP_ACCOUNT_HEIRLOOMS, "REPLACE INTO battlenet_account_heirlooms (accountId, itemId, flags) VALUES (?, ?, ?)");

		// Account wide mounts
		PrepareStatement(LoginStatements.SEL_ACCOUNT_MOUNTS, "SELECT mountSpellId, flags FROM battlenet_account_mounts WHERE battlenetAccountId = ?");
		PrepareStatement(LoginStatements.REP_ACCOUNT_MOUNTS, "REPLACE INTO battlenet_account_mounts (battlenetAccountId, mountSpellId, flags) VALUES (?, ?, ?)");

		// Transmog collection
		PrepareStatement(LoginStatements.SEL_BNET_ITEM_APPEARANCES, "SELECT blobIndex, appearanceMask FROM battlenet_item_appearances WHERE battlenetAccountId = ? ORDER BY blobIndex DESC");
		PrepareStatement(LoginStatements.INS_BNET_ITEM_APPEARANCES, "INSERT INTO battlenet_item_appearances (battlenetAccountId, blobIndex, appearanceMask) VALUES (?, ?, ?) ON DUPLICATE KEY UPDATE appearanceMask = appearanceMask | VALUES(appearanceMask)");
		PrepareStatement(LoginStatements.SEL_BNET_ITEM_FAVORITE_APPEARANCES, "SELECT itemModifiedAppearanceId FROM battlenet_item_favorite_appearances WHERE battlenetAccountId = ?");
		PrepareStatement(LoginStatements.INS_BNET_ITEM_FAVORITE_APPEARANCE, "INSERT INTO battlenet_item_favorite_appearances (battlenetAccountId, itemModifiedAppearanceId) VALUES (?, ?)");
		PrepareStatement(LoginStatements.DEL_BNET_ITEM_FAVORITE_APPEARANCE, "DELETE FROM battlenet_item_favorite_appearances WHERE battlenetAccountId = ? AND itemModifiedAppearanceId = ?");
		PrepareStatement(LoginStatements.SEL_BNET_TRANSMOG_ILLUSIONS, "SELECT blobIndex, illusionMask FROM battlenet_account_transmog_illusions WHERE battlenetAccountId = ? ORDER BY blobIndex DESC");
		PrepareStatement(LoginStatements.INS_BNET_TRANSMOG_ILLUSIONS, "INSERT INTO battlenet_account_transmog_illusions (battlenetAccountId, blobIndex, illusionMask) VALUES (?, ?, ?) ON DUPLICATE KEY UPDATE illusionMask = illusionMask | VALUES(illusionMask)");


		PrepareStatement(LoginStatements.LOGIN_SEL_BATTLE_PAY_ACCOUNT_CREDITS, "SELECT `battlePayCredits` from battlenet_accounts WHERE id = ?;");
		PrepareStatement(LoginStatements.LOGIN_UPD_BATTLE_PAY_ACCOUNT_CREDITS, "UPDATE battlenet_accounts SET battlePayCredits = ? WHERE id = ?;");
		PrepareStatement(LoginStatements.LOGIN_INS_PURCHASE, "INSERT INTO battlepay_purchases (battlenetAccountId, realm, characterGuid, productID, productName, CurrentPrice, RemoteAddress) VALUES (?, ?, ?, ?, ?, ?, ?)");

		/// Seraphim
		PrepareStatement(LoginStatements.LOGIN_SEL_BNET_RUNEFORGE_MEMORIES, "SELECT blobIndex, runeForgeMask FROM battlenet_account_runeforge_memories WHERE battlenetAccountId = ? ORDER BY blobIndex DESC");
		PrepareStatement(LoginStatements.LOGIN_INS_BNET_RUNEFORGE_MEMORIES, "INSERT INTO battlenet_account_runeforge_memories (battlenetAccountId, blobIndex, runeForgeMask) VALUES (?, ?, ?) ON DUPLICATE KEY UPDATE runeForgeMask = runeForgeMask | VALUES(runeForgeMask)");
	}
}