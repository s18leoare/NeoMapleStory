﻿using NeoMapleStory.Core;
using NeoMapleStory.Core.Database;
using NeoMapleStory.Core.IO;
using NeoMapleStory.Core.TimeManager;
using NeoMapleStory.Game.Buff;
using NeoMapleStory.Game.Client.AntiCheat;
using NeoMapleStory.Game.Inventory;
using NeoMapleStory.Game.Job;
using NeoMapleStory.Game.Map;
using NeoMapleStory.Game.Mob;
using NeoMapleStory.Game.Movement;
using NeoMapleStory.Game.Quest;
using NeoMapleStory.Game.Shop;
using NeoMapleStory.Game.Skill;
using NeoMapleStory.Game.World;
using NeoMapleStory.Packet;
using NeoMapleStory.Server;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NeoMapleStory.Game.Script.Event;
using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace NeoMapleStory.Game.Client
{
    public class MapleCharacter : AbstractAnimatedMapleMapObject
    {

        public static double MaxViewRangeSq { get; } = 850 * 850;

        //Character Info Begin
        public Account Account { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsMarried { get; set; }
        public byte GmLevel { get; set; }
        public byte WorldId { get; set; }
        public int Face { get; set; }
        public int Hair { get; set; }
        public short RemainingAp { get; set; }
        public short RemainingSp { get; set; }
        public short Fame { get; set; }
        public short Hp { get; set; }
        public short MaxHp { get; set; }
        public short Mp { get; set; }
        public short MaxMp { get; set; }
        public byte Level { get; set; }
        public MapleSkinColor Skin { get; set; } = MapleSkinColor.Normal;
        public short Dex { get; set; }
        public short Int { get; set; }
        public short Luk { get; set; }
        public short Str { get; set; }
        public int AutoHpPot { get; set; }
        public int AutoMpPot { get; set; }
        public byte InitialSpawnPoint { get; set; }
        public bool Gender { get; set; }
        //Character Info End

        //计算出的属性
      public short   Localmaxhp { get; set; }
        public short Localmaxmp { get; set; }
        public short Localdex { get; set; }
        public short Localint { get; set; }
        public short Localstr { get; set; }
        public short Localluk { get; set; }
        public int Watk { get; set; }
        public int Matk { get; set; }
        public int Wdef { get; set; }
        public int Mdef { get; set; }
        public int Magic { get; set; }
        public double SpeedMod { get; set; }
        public double JumpMod { get; set; }














        //public CharacterModel CharacterInfo { get; set; }


        private int _apqScore;
        private int _cp;
        private readonly Dictionary<MapleBuffStat, MapleBuffStatValueHolder> _effects = new Dictionary<MapleBuffStat, MapleBuffStatValueHolder>();
        private readonly List<MapleDisease> _diseases = new List<MapleDisease>();
        public Dictionary<ISkill, SkillEntry> Skills { get; private set; } = new Dictionary<ISkill, SkillEntry>();
        private readonly List<string> _mapletips = new List<string>();
        private readonly Dictionary<int, MapleCoolDownValueHolder> _coolDowns = new Dictionary<int, MapleCoolDownValueHolder>();
        private readonly Dictionary<MapleQuest, MapleQuestStatus> _quests = new Dictionary<MapleQuest, MapleQuestStatus>();
        private readonly SkillMacro[] _skillMacros = new SkillMacro[5];
        //private IPlayerInteractionManager interaction = null;
        private readonly List<MapleMonster> _controlled = new List<MapleMonster>();

        public int JobType => Job.JobId / 2000;
        public bool IsAlive => Hp > 0;

        public InterLockedInt Exp { get; set; } = new InterLockedInt(0);

        public InterLockedInt Money { get; set; } = new InterLockedInt(0);

        public MapleClient Client { get; set; }

        public MapleMap Map { get; set; }

        public MapleJob Job { get; set; }

        public MapleInventory[] Inventorys { get; set; }

        public MapleParty Party { get; set; }

        public List<MapleDoor> Doors { get; set; }

        public MapleMessenger Messenger { get; set; }

        public bool IsHidden { get; set; }

        public MapleShop Shop { get; set; }

        public MapleBuddyList Buddies { get; set; } = new MapleBuddyList(20);

        public int ItemEffect { get; set; }

        public int Chair { get; set; }

        public List<MaplePet> Pets { get; set; } = new List<MaplePet>();

        public Dictionary<int, MapleSummon> Summons { get; set; } = new Dictionary<int, MapleSummon>();

        public List<IMapleMapObject> VisibleMapObjects { get; set; } = new List<IMapleMapObject>();

        public List<ILifeMovementFragment> Lastres { get; set; } = new List<ILifeMovementFragment>();
        private string _chalkboardtext;

        public string ChalkBoardText
        {
            get { return _chalkboardtext; }
            set
            {
                //if (interaction != null)
                //{
                //    return;
                //}
                _chalkboardtext = value;
                if (_chalkboardtext == null)
                {
                    Map.BroadcastMessage(PacketCreator.UseChalkboard(this, true));
                }
                else {
                    Map.BroadcastMessage(PacketCreator.UseChalkboard(this, false));
                }
            }
        }

        public int Energybar { get; set; } = 0;

        public EventInstanceManager EventInstanceManager { get; set; }

        public CheatTracker AntiCheatTracker { get; private set; }

        public MapleCharacter()
        {
            Stance = 0;
            Inventorys = new MapleInventory[MapleInventoryType.TypeList.Length];
            foreach (var type in MapleInventoryType.TypeList)
            {
                Inventorys[type.Value] = new MapleInventory(type, 100);
            }
            Position = Point.Empty;
            AntiCheatTracker = new CheatTracker(this);
        }

        public static MapleCharacter GetDefault(Account account)
        {
            MapleCharacter ret = new MapleCharacter();
            ret.Account = account;
            ret.Inventorys[MapleInventoryType.Equip.Value] = new MapleInventory(MapleInventoryType.Equip, 24);
            ret.Inventorys[MapleInventoryType.Use.Value] = new MapleInventory(MapleInventoryType.Use, 24);
            ret.Inventorys[MapleInventoryType.Setup.Value] = new MapleInventory(MapleInventoryType.Setup, 24);
            ret.Inventorys[MapleInventoryType.Etc.Value] = new MapleInventory(MapleInventoryType.Etc, 24);
            ret.Inventorys[MapleInventoryType.Cash.Value] = new MapleInventory(MapleInventoryType.Cash, 50);


           ret. Hp = 50;
            ret.MaxHp = 50;
            ret.Mp = 50;
            ret.MaxMp = 50;
            ret.Exp.Reset();
            ret.GmLevel = 0;
            ret.Money.Exchange(10000);
            ret.Level = 1;
                //BookCover = 0,
                //Maplemount = null,
                //TotalCp = 0,
                //Team = -1,
                //Incs = false,
                //Inmts = false,
                //AllianceRank = 5,
                //AccountId = accountId



            ret.Map = null;

            ret.Job = MapleJob.Beginner;// MapleJob.BEGINNER;

            ret.Buddies = new MapleBuddyList(20);

            ret._cp = 0;

            ret._apqScore = 0;


            //ret.keymap.put(Integer.valueOf(2), new MapleKeyBinding(4, 10));
            //ret.keymap.put(Integer.valueOf(3), new MapleKeyBinding(4, 12));
            //ret.keymap.put(Integer.valueOf(4), new MapleKeyBinding(4, 13));
            //ret.keymap.put(Integer.valueOf(5), new MapleKeyBinding(4, 18));
            //ret.keymap.put(Integer.valueOf(6), new MapleKeyBinding(4, 24));
            //ret.keymap.put(Integer.valueOf(7), new MapleKeyBinding(4, 21));
            //ret.keymap.put(Integer.valueOf(16), new MapleKeyBinding(4, 8));
            //ret.keymap.put(Integer.valueOf(17), new MapleKeyBinding(4, 5));
            //ret.keymap.put(Integer.valueOf(18), new MapleKeyBinding(4, 0));
            //ret.keymap.put(Integer.valueOf(19), new MapleKeyBinding(4, 4));
            //ret.keymap.put(Integer.valueOf(23), new MapleKeyBinding(4, 1));
            //ret.keymap.put(Integer.valueOf(25), new MapleKeyBinding(4, 19));
            //ret.keymap.put(Integer.valueOf(26), new MapleKeyBinding(4, 14));
            //ret.keymap.put(Integer.valueOf(27), new MapleKeyBinding(4, 15));
            //ret.keymap.put(Integer.valueOf(29), new MapleKeyBinding(5, 52));
            //ret.keymap.put(Integer.valueOf(31), new MapleKeyBinding(4, 2));
            //ret.keymap.put(Integer.valueOf(34), new MapleKeyBinding(4, 17));
            //ret.keymap.put(Integer.valueOf(35), new MapleKeyBinding(4, 11));
            //ret.keymap.put(Integer.valueOf(37), new MapleKeyBinding(4, 3));
            //ret.keymap.put(Integer.valueOf(38), new MapleKeyBinding(4, 20));
            //ret.keymap.put(Integer.valueOf(40), new MapleKeyBinding(4, 16));
            //ret.keymap.put(Integer.valueOf(41), new MapleKeyBinding(4, 23));
            //ret.keymap.put(Integer.valueOf(43), new MapleKeyBinding(4, 9));
            //ret.keymap.put(Integer.valueOf(44), new MapleKeyBinding(5, 50));
            //ret.keymap.put(Integer.valueOf(45), new MapleKeyBinding(5, 51));
            //ret.keymap.put(Integer.valueOf(46), new MapleKeyBinding(4, 6));
            //ret.keymap.put(Integer.valueOf(48), new MapleKeyBinding(4, 22));
            //ret.keymap.put(Integer.valueOf(50), new MapleKeyBinding(4, 7));
            //ret.keymap.put(Integer.valueOf(56), new MapleKeyBinding(5, 53));
            //ret.keymap.put(Integer.valueOf(57), new MapleKeyBinding(5, 54));
            //ret.keymap.put(Integer.valueOf(59), new MapleKeyBinding(6, 100));
            //ret.keymap.put(Integer.valueOf(60), new MapleKeyBinding(6, 101));
            //ret.keymap.put(Integer.valueOf(61), new MapleKeyBinding(6, 102));
            //ret.keymap.put(Integer.valueOf(62), new MapleKeyBinding(6, 103));
            //ret.keymap.put(Integer.valueOf(63), new MapleKeyBinding(6, 104));
            //ret.keymap.put(Integer.valueOf(64), new MapleKeyBinding(6, 105));
            //ret.keymap.put(Integer.valueOf(65), new MapleKeyBinding(6, 106));

            ret.RecalcLocalStats();

            return ret;
        }
        private void RecalcLocalStats()
        {
            int oldmaxhp = Localmaxhp;
            Localmaxhp = MaxHp;
            Localmaxmp = MaxMp;
            Localdex = Dex;
            Localint = Int;
            Localstr = Str;
            Localluk = Luk;
            int speed = 100;
            int jump = 100;
            Magic = Localint;
            Watk = 0;

            foreach (var item in Inventorys[MapleInventoryType.Equipped.Value].Inventory.Values)
            {
                IEquip equip = (IEquip)item;
                Localmaxhp += equip.Hp;
                Localmaxmp += equip.Mp;
                Localdex += equip.Dex;
                Localint += equip.Int;
                Localstr += equip.Str;
                Localluk += equip.Luk;
                Magic += equip.Matk + equip.Int;
                Watk += equip.Watk;
                speed += equip.Speed;
                jump += equip.Jump;
            }

            IMapleItem weapon;
            if (!Inventorys[MapleInventoryType.Equipped.Value].Inventory.TryGetValue(11, out weapon) && Job == MapleJob.Pirate)
            {
                // Barefists
                Watk += 8;
            }
            Magic = Math.Min(Magic, 2000);

            int? hbhp = GetBuffedValue(MapleBuffStat.Hyperbodyhp);
            if (hbhp.HasValue)
            {
                Localmaxhp += (short)(hbhp.Value / 100 * Localmaxhp);
            }
            int? hbmp = GetBuffedValue(MapleBuffStat.Hyperbodymp);
            if (hbmp.HasValue)
            {
                Localmaxmp += (short)(hbmp.Value / 100 * Localmaxmp);
            }

            Localmaxhp = Math.Min((short)30000, Localmaxhp);
            Localmaxmp = Math.Min((short)30000, Localmaxmp);

            var watkbuff = GetBuffedValue(MapleBuffStat.Watk);
            if (watkbuff.HasValue)
            {
                Watk += watkbuff.Value;
            }
            if (Job == MapleJob.Bowman)
            {
                ISkill expert = null;
                if (Job == MapleJob.Crossbowmaster)
                {
                    expert = SkillFactory.GetSkill(3220004);
                }
                else if (Job == MapleJob.Bowmaster)
                {
                    expert = SkillFactory.GetSkill(3120005);
                }
                if (expert != null)
                {
                    int boostLevel = getSkillLevel(expert);
                    if (boostLevel > 0)
                    {
                        Watk += expert.GetEffect(boostLevel).X;
                    }
                }
            }

            var matkbuff = GetBuffedValue(MapleBuffStat.Matk);
            if (matkbuff.HasValue)
            {
                Magic += matkbuff.Value;
            }

            var speedbuff = GetBuffedValue(MapleBuffStat.Speed);
            if (speedbuff.HasValue)
            {
                speed += speedbuff.Value;
            }

            var jumpbuff = GetBuffedValue(MapleBuffStat.Jump);
            if (jumpbuff.HasValue)
            {
                jump += jumpbuff.Value;
            }

            if (speed > 140)
            {
                speed = 140;
            }
            if (jump > 123)
            {
                jump = 123;
            }

            SpeedMod = speed / 100.0;
            JumpMod = jump / 100.0;

            var mount = GetBuffedValue(MapleBuffStat.MonsterRiding);
            if (mount.HasValue)
            {
                JumpMod = 1.23;
                switch (mount.Value)
                {
                    case 1:
                        SpeedMod = 1.5;
                        break;
                    case 2:
                        SpeedMod = 1.7;
                        break;
                    case 3:
                        SpeedMod = 1.8;
                        break;
                    case 5:
                        SpeedMod = 1.0;
                        JumpMod = 1.0;
                        break;
                    default:
                        SpeedMod = 2.0;
                        break;
                }
            }


            var mWarrior = GetBuffedValue(MapleBuffStat.MapleWarrior);
            if (mWarrior.HasValue)
            {
                Localstr += (short)(mWarrior.Value / 100 * Localstr);
                Localdex += (short)(mWarrior.Value / 100 * Localdex);
                Localint += (short)(mWarrior.Value / 100 * Localint);
                Localluk += (short)(mWarrior.Value / 100 * Localluk);
            }

            // localmaxbasedamage = calculateMaxBaseDamage(Watk);
            if (oldmaxhp != 0 && oldmaxhp != Localmaxhp)
            {
                UpdatePartyMemberHp();
            }
        }


        public void GainNexonPoint(int value)
        {
            /*Account.NexonPoint += value;*/
        }



        public void SaveToDb(bool update)
        {
            MySqlCommand cmd;
            if (update)
                cmd = new MySqlCommand("UPDATE Character SET Level = @Level, Fame = @Fame, Str =@Str , Dex = @Dex, Luk =@Luk, Int =@Int, EXP =@Exp, Hp = @Hp, Mp =@Mp, MaxHp =@MaxHp, MaxMp = @MaxMp, RemainingSp =@RemainingSP, RemainingAp =@RemainingAp, GmLevel =@GmLevel, Skin = @Skin,JobId =@JobId, Hair =@Hair, Face =@Face, MapId = @MapId, Money = @Money, SpawnPoint = @SpawnPoint, PartyId = @PartyId, AutoHpPot = @AutoHpPot, AutoMpPot = @AutoMpPot, IsMarried = @IsMarried, EquipSlots = @EquipSlots, UseSlots = @UseSlots, SetupSlots = @SetupSlots, EtcSlots = @EtcSlots, CashSlots = @CashSlots WHERE Id = @Id");
            else
                cmd = new MySqlCommand(
                    @"INSERT Character(AId,WorldId,Name,JobId,GmLevel,Level,Dex,Int,Luk,Str,Hp,MaxHp,Mp,MaxMp,EXP,Money,Fame,IsMarried,MapId,SpawnPoint,PartyId,Face,Hair,Skin,RemainingAp,RemainingSp,AutoHpPot,AutoMpPot,EquipSlots,UseSlots,SetupSlots,EtcSlots,CashSlots)
VALUES(@AId,@WorldId,@Name,@JobId,@GmLevel,@Level,@Dex,@Int,@Luk,@Str,@Hp,@MaxHp,@Mp,@MaxMp,@Exp,@Money,@Fame,@IsMarried,@MapId,@SpawnPoint,@PartyId,@Face,@Hair,@Skin,@RemainingAp,@RemainingSp,@AutoHpPot,@AutoMpPot,@EquipSlots,@UseSlots,@SetupSlots,@EtcSlots,@CashSlots)");


            cmd.Parameters.Add(new SqlParameter("@Level", Level));
            cmd.Parameters.Add(new SqlParameter("@Fame", Fame));
            cmd.Parameters.Add(new SqlParameter("@Str", Str));
            cmd.Parameters.Add(new SqlParameter("@Dex", Dex));
            cmd.Parameters.Add(new SqlParameter("@Luk", Luk));
            cmd.Parameters.Add(new SqlParameter("@Int", Int));
            cmd.Parameters.Add(new SqlParameter("@Exp", Exp.Value));
            cmd.Parameters.Add(new SqlParameter("@Hp", Hp));
            cmd.Parameters.Add(new SqlParameter("@Mp", Mp));
            cmd.Parameters.Add(new SqlParameter("@MaxHp", MaxHp));
            cmd.Parameters.Add(new SqlParameter("@MaxMp", MaxMp));
            cmd.Parameters.Add(new SqlParameter("@RemainingSp", RemainingSp));
            cmd.Parameters.Add(new SqlParameter("@RemainingAp", RemainingAp));
            cmd.Parameters.Add(new SqlParameter("@GmLevel", GmLevel));
            cmd.Parameters.Add(new SqlParameter("@Skin", Skin.ColorId));
            cmd.Parameters.Add(new SqlParameter("@JobId", Job.JobId));
            cmd.Parameters.Add(new SqlParameter("@Hair", Hair));
            cmd.Parameters.Add(new SqlParameter("@Face", Face));
            cmd.Parameters.Add(new SqlParameter("@Money", Money.Value));
            //ps.setInt(22, hpApUsed);
            //ps.setInt(23, mpApUsed);


            int mapId = 0;
            if (Map == null)
            {
                if (!update && Job.JobId == 1000)
                    mapId = 130030000;
                else if (!update && Job.JobId == 2000)
                    mapId = 914000000;
                else
                    mapId = 0;
            }
            else if (Map.MapId == 677000013 || Map.MapId == 677000012)
            {
                mapId = 970030002;
            }
            else if (Map.ForcedReturnMapId != 999999999)
            {
                mapId = Map.ForcedReturnMapId;
            }
            else
            {
                 mapId = Map.MapId;
            }
               
            cmd.Parameters.Add(new SqlParameter("@MapId", mapId));

            int spawnPoint = 0;
            if (Map == null || Map.MapId == 610020000 || Map.MapId == 610020001)
            {
                spawnPoint = 0;
            }
            else
            {
                IMaplePortal closest = Map.FindClosestSpawnpoint(Position);
                spawnPoint = closest?.PortalId ?? 0;
            }

            cmd.Parameters.Add(new SqlParameter("@SpawnPoint", spawnPoint));

            cmd.Parameters.Add(new SqlParameter("@PartyId", Party?.PartyId ?? 0));

            cmd.Parameters.Add(new SqlParameter("@AutoHpPot", AutoHpPot != 0 && GetItemAmount(AutoHpPot) >= 1 ? AutoHpPot : 0));

            cmd.Parameters.Add(new SqlParameter("@AutoMpPot", AutoMpPot != 0 && GetItemAmount(AutoMpPot) >= 1 ? AutoMpPot : 0));

            cmd.Parameters.Add(new SqlParameter("@IsMarried", IsMarried));


            //if (Messenger == null)
            //{
            //    MessengerId = 0;
            //    MessengerPosition = 4;
            //}



            //if (ZakumLvl > 2)
            //    ZakumLvl = 2;


            //if (maplemount != null)
            //{
            //    ps.setInt(49, maplemount.getLevel());
            //    ps.setInt(50, maplemount.getExp());
            //    ps.setInt(51, maplemount.getTiredness());
            //}
            //else {
            //    ps.setInt(49, 1);
            //    ps.setInt(50, 0);
            //    ps.setInt(51, 0);
            //}

            cmd.Parameters.Add(new SqlParameter("@EquipSlots", Inventorys[MapleInventoryType.Equip.Value].SlotLimit));
            cmd.Parameters.Add(new SqlParameter("@UseSlots", Inventorys[MapleInventoryType.Equip.Value].SlotLimit));
            cmd.Parameters.Add(new SqlParameter("@SetupSlots", Inventorys[MapleInventoryType.Setup.Value].SlotLimit));
            cmd.Parameters.Add(new SqlParameter("@EtcSlots", Inventorys[MapleInventoryType.Etc.Value].SlotLimit));
            cmd.Parameters.Add(new SqlParameter("@CashSlots", Inventorys[MapleInventoryType.Cash.Value].SlotLimit));

            if (update)
            {
                cmd.Parameters.Add(new SqlParameter("@Id", Id));
            }
            else
            {
                cmd.Parameters.Add(new SqlParameter("@AId", Account.Id));
                cmd.Parameters.Add(new SqlParameter("@Name",Name));
                cmd.Parameters.Add(new SqlParameter("@WorldId", WorldId));
            }


            using (var con = DbConnectionManager.Instance.GetConnection())
            {
                cmd.Connection = con;
                con.Open();
                cmd.Transaction = con.BeginTransaction(IsolationLevel.ReadUncommitted);

                int updateRow = cmd.ExecuteNonQuery();
                
                if (!update)
                {
                    Id = Convert.ToInt32(cmd.LastInsertedId);
                    cmd.Parameters.Clear();
                }
                else if (updateRow < 1)
                {
                    throw new Exception("角色不存在于数据库中");
                }

                //foreach (MaplePet pettt in Pets)
                //{
                //    pettt.SaveToDb();
                //}

                //技能宏指令

                //ps = con.prepareStatement("DELETE FROM skillmacros WHERE characterid = ?");
                //ps.setInt(1, id);
                //ps.executeUpdate();
                //ps.close();

                //for (int i = 0; i < 5; i++)
                //{
                //    SkillMacro macro = skillMacros[i];
                //    if (macro != null)
                //    {
                //        ps = con.prepareStatement("INSERT INTO skillmacros (characterid, skill1, skill2, skill3, name, shout, position) VALUES (?, ?, ?, ?, ?, ?, ?)");

                //        ps.setInt(1, id);
                //        ps.setInt(2, macro.getSkill1());
                //        ps.setInt(3, macro.getSkill2());
                //        ps.setInt(4, macro.getSkill3());
                //        ps.setString(5, macro.getName());
                //        ps.setInt(6, macro.getShout());
                //        ps.setInt(7, i);

                //        ps.executeUpdate();
                //        ps.close();
                //    }
                //}
                //if (csinventory != null)
                //{
                //    getCSInventory().saveToDB();
                //}


                cmd.CommandText = "DELETE FROM InventoryItem WHERE CId=@CId";
                cmd.Parameters.Add(new SqlParameter("@CId", Id));
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                string cmdStr_Item = "INSERT InventoryItem(CId,ItemId,Type,Position,Quantity,Owner,ExpireDate,UniqueId) VALUES(@CId,@ItemId,@Type,@Position,@Quantity,@Owner,@ExpireDate,@UniqueId)";
                string cmdStr_Equipment =
                    @"INSERT InventoryEquipment(InventoryItemId,UpgradeSlots,Level,Str,Dex,Int,Luk,HP,MP,Watk,Matk,Wdef,Mdef,Acc,Avoid,Hands,Speed,Jump,Vicious,IsRing,Flag,Locked) 
VALUES(@InventoryItemId,@UpgradeSlots,@Level,@Str,@Dex,@Int,@Luk,@HP,@MP,@Watk,@Matk,@Wdef,@Mdef,@Acc,@Avoid,@Hands,@Speed,@Jump,@Vicious,@IsRing,@Flag,@Locked)";

                foreach (var inventory in Inventorys)
                {
                    //ps.setInt(3, iv.getType().getType());
                    foreach (var item in inventory.Inventory.Values)
                    {
                        cmd.CommandText = cmdStr_Item;
                        cmd.Parameters.Add(new SqlParameter("@CId", Id));
                        cmd.Parameters.Add(new SqlParameter("@ItemId", item.ItemId));
                        cmd.Parameters.Add(new SqlParameter("@Type", inventory.Type.Value));
                        cmd.Parameters.Add(new SqlParameter("@Position", item.Position));
                        cmd.Parameters.Add(new SqlParameter("@Quantity", item.Quantity));
                        cmd.Parameters.Add(new SqlParameter("@Owner", item.Owner ?? (object) DBNull.Value));
                        cmd.Parameters.Add(new SqlParameter("@UniqueId", item.UniqueId));
                        cmd.Parameters.Add(new SqlParameter("@ExpireDate", item.Expiration ?? (object)DBNull.Value));
                        //PetSlot = /*getPetByUniqueId(item.UniqueID) > -1 ? getPetByUniqueId(item.UniqueID) + 1 :*/ 0,
                        cmd.ExecuteNonQuery();
                    
                        int inventoryId =  Convert.ToInt32(cmd.LastInsertedId);
                        cmd.Parameters.Clear();

                        if (inventory.Type == MapleInventoryType.Equip || inventory.Type == MapleInventoryType.Equipped)
                        {
                            IEquip equip = (IEquip)item;
                            cmd.CommandText = cmdStr_Equipment;
                            cmd.Parameters.Add(new SqlParameter("@InventoryItemId", inventoryId));
                            cmd.Parameters.Add(new SqlParameter("@Acc", equip.Acc));
                            cmd.Parameters.Add(new SqlParameter("@Avoid", equip.Avoid));
                            cmd.Parameters.Add(new SqlParameter("@Dex", equip.Dex));
                            cmd.Parameters.Add(new SqlParameter("@Hands", equip.Hands));
                            cmd.Parameters.Add(new SqlParameter("@Hp", equip.Hp));
                            cmd.Parameters.Add(new SqlParameter("@Mp", equip.Mp));
                            cmd.Parameters.Add(new SqlParameter("@Int", equip.Int));
                            cmd.Parameters.Add(new SqlParameter("@IsRing", equip.IsRing));
                            cmd.Parameters.Add(new SqlParameter("@Jump", equip.Jump));
                            cmd.Parameters.Add(new SqlParameter("@Level", equip.Level));
                            cmd.Parameters.Add(new SqlParameter("@Locked", equip.Locked));
                            cmd.Parameters.Add(new SqlParameter("@Luk", equip.Luk));
                            cmd.Parameters.Add(new SqlParameter("@Matk", equip.Matk));
                            cmd.Parameters.Add(new SqlParameter("@Mdef", equip.Mdef));
                            cmd.Parameters.Add(new SqlParameter("@Watk", equip.Watk));
                            cmd.Parameters.Add(new SqlParameter("@Wdef", equip.Wdef));
                            cmd.Parameters.Add(new SqlParameter("@Speed", equip.Speed));
                            cmd.Parameters.Add(new SqlParameter("@Str", equip.Str));
                            cmd.Parameters.Add(new SqlParameter("@UpgradeSlots", equip.UpgradeSlots));
                            cmd.Parameters.Add(new SqlParameter("@Vicious", equip.Vicious));
                            cmd.Parameters.Add(new SqlParameter("@Flag", equip.Flag));
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                        }
                    }
                }

                //deleteWhereCharacterId(con, "DELETE FROM queststatus WHERE characterid = ?");
                //ps = con.prepareStatement("INSERT INTO queststatus (`queststatusid`, `characterid`, `quest`, `status`, `time`, `forfeited`) VALUES (DEFAULT, ?, ?, ?, ?, ?)", Statement.RETURN_GENERATED_KEYS);
                //pse = con.prepareStatement("INSERT INTO queststatusmobs VALUES (DEFAULT, ?, ?, ?)");
                //ps.setInt(1, id);
                //for (MapleQuestStatus q : quests.values())
                //{
                //    ps.setInt(2, q.getQuest().getId());
                //    ps.setInt(3, q.getStatus().getId());
                //    ps.setInt(4, (int)(q.getCompletionTime() / 1000));
                //    ps.setInt(5, q.getForfeited());
                //    ps.executeUpdate();
                //    ResultSet rs = ps.getGeneratedKeys();
                //    rs.next();
                //    for (int mob : q.getMobKills().keySet())
                //    {
                //        pse.setInt(1, rs.getInt(1));
                //        pse.setInt(2, mob);
                //        pse.setInt(3, q.getMobKills(mob));
                //        pse.executeUpdate();
                //    }
                //    rs.close();
                //}
                //ps.close();
                //pse.close();

                //deleteWhereCharacterId(con, "DELETE FROM skills WHERE characterid = ?");
                //ps = con.prepareStatement("INSERT INTO skills (characterid, skillid, skilllevel, masterlevel) VALUES (?, ?, ?, ?)");
                //ps.setInt(1, id);
                //for (Entry<ISkill, SkillEntry> skill : skills.entrySet())
                //{
                //    ps.setInt(2, skill.getKey().getId());
                //    ps.setInt(3, skill.getValue().skillevel);
                //    ps.setInt(4, skill.getValue().masterlevel);
                //    ps.executeUpdate();
                //}
                //ps.close();

                //deleteWhereCharacterId(con, "DELETE FROM keymap WHERE characterid = ?");
                //ps = con.prepareStatement("INSERT INTO keymap (characterid, `key`, `type`, `action`) VALUES (?, ?, ?, ?)");
                //ps.setInt(1, id);
                //for (Entry<Integer, MapleKeyBinding> keybinding : keymap.entrySet())
                //{
                //    ps.setInt(2, keybinding.getKey().intValue());
                //    ps.setInt(3, keybinding.getValue().getType());
                //    ps.setInt(4, keybinding.getValue().getAction());
                //    ps.executeUpdate();
                //}
                //ps.close();

                //deleteWhereCharacterId(con, "DELETE FROM savedlocations WHERE characterid = ?");
                //ps = con.prepareStatement("INSERT INTO savedlocations (characterid, `locationtype`, `map`) VALUES (?, ?, ?)");
                //ps.setInt(1, id);
                //for (SavedLocationType savedLocationType : SavedLocationType.values())
                //{
                //    if (savedLocations[savedLocationType.ordinal()] != -1)
                //    {
                //        ps.setString(2, savedLocationType.name());
                //        ps.setInt(3, savedLocations[savedLocationType.ordinal()]);
                //        ps.executeUpdate();
                //    }
                //}
                //ps.close();

                //deleteWhereCharacterId(con, "DELETE FROM buddies WHERE characterid = ? AND pending = 0");
                //ps = con.prepareStatement("INSERT INTO buddies (characterid, `buddyid`, `group`, `pending`) VALUES (?, ?, ?, 0)");
                //ps.setInt(1, id);
                //for (BuddylistEntry entry : buddylist.getBuddies())
                //{
                //    if (entry.isVisible())
                //    {
                //        ps.setInt(2, entry.getCharacterId());
                //        ps.setString(3, entry.getGroup());
                //        ps.executeUpdate();
                //    }
                //}
                //ps.close();

                //ps = con.prepareStatement("UPDATE accounts SET `paypalNX` = ?, `mPoints` = ?, `cardNX` = ?, `Present` = ? WHERE id = ?");
                //ps.setInt(1, paypalnx);
                //ps.setInt(2, maplepoints);
                //ps.setInt(3, cardnx);
                //ps.setInt(4, Present);
                //ps.setInt(5, client.getAccID());
                //ps.executeUpdate();
                //ps.close();

                //if (storage != null)
                //{
                //    storage.saveToDB();
                //}

                //if (update)
                //{
                //    ps = con.prepareStatement("DELETE FROM achievements WHERE accountid = ?");
                //    ps.setInt(1, accountid);
                //    ps.executeUpdate();
                //    ps.close();

                //    for (Integer achid : finishedAchievements)
                //    {
                //        ps = con.prepareStatement("INSERT INTO achievements(charid, achievementid, accountid) VALUES(?, ?, ?)");
                //        ps.setInt(1, id);
                //        ps.setInt(2, achid);
                //        ps.setInt(3, accountid);
                //        ps.executeUpdate();
                //        ps.close();
                //    }
                //}


                cmd.Transaction.Commit();
            }
        }

        public void StartMapEffect(string msg, int itemId, int duration = 30000)
        {
            MapleMapEffect mapEffect = new MapleMapEffect(msg, itemId);
            Client.Send(mapEffect.CreateStartData());
            TimerManager.Instance.ScheduleJob(() =>
            {
                Client.Send(mapEffect.CreateDestroyData());
            }, duration);
        }

        public static MapleCharacter LoadCharFromDb(int charid, MapleClient client, bool channelserver)
        {
            MapleCharacter ret = new MapleCharacter();
            ret.Client = client;

            MySqlCommand cmd = new MySqlCommand("SELECT * FROM Character WHERE Id=@Id");
            cmd.Parameters.Add(new MySqlParameter("@Id", charid));

            using (var con = DbConnectionManager.Instance.GetConnection())
            {
                cmd.Connection = con;
                con.Open();

                var reader = cmd.ExecuteReader(CommandBehavior.SingleRow);

                if (!reader.Read())
                    throw new Exception("加载角色失败（未找到角色）");

                ret.Id = (int)reader["Id"];
                ret.WorldId = (byte)reader["WorldId"];

                ret.Name = (string)reader["Name"];
                ret.Level = (byte)reader["Level"];
                ret.Fame = (short)reader["Fame"];
                ret.Str = (short)reader["Str"];
                ret.Dex = (short)reader["Dex"];
                ret.Int = (short)reader["Int"];
                ret.Luk = (short)reader["Luk"];
                ret.Exp.Exchange((int)reader["EXP"]);
                ret.Money.Exchange((int)reader["Money"]);
                ret.Hp = (short)reader["HP"];
                ret.Mp = (short)reader["MP"];
                ret.MaxHp = (short)reader["MaxHP"];
                ret.MaxMp = (short)reader["MaxMP"];

                ret.RemainingAp = (short)reader["RemainingAP"];
                ret.RemainingSp = (short)reader["RemainingSP"];

                ret.GmLevel = (byte)reader["GmLevel"];

                ret.Job = new MapleJob((short)reader["JobId"]);
                ret.Skin = new MapleSkinColor((byte)reader["Skin"]);
                ret.IsMarried = (bool)reader["IsMarried"];
                ret.Hair = (int)reader["Hair"];
                ret.Face = (int)reader["Face"];

                ret.AutoHpPot = (int)reader["AutoHPPot"];
                ret.AutoMpPot = (int)reader["AutoMPPot"];

                ret.Inventorys[MapleInventoryType.Equip.Value] = new MapleInventory(MapleInventoryType.Equip,
                    (byte)reader["EquipSlots"]);
                ret.Inventorys[MapleInventoryType.Use.Value] = new MapleInventory(MapleInventoryType.Use,
                    (byte)reader["UseSlots"]);
                ret.Inventorys[MapleInventoryType.Setup.Value] = new MapleInventory(MapleInventoryType.Setup,
                    (byte)reader["SetupSlots"]);
                ret.Inventorys[MapleInventoryType.Etc.Value] = new MapleInventory(MapleInventoryType.Etc,
                    (byte)reader["EtcSlots"]);
                ret.Inventorys[MapleInventoryType.Cash.Value] = new MapleInventory(MapleInventoryType.Cash,
                    (byte)reader["CashSlots"]);

                //int mountexp = rs.getInt("mountexp");
                //int mountlevel = rs.getInt("mountlevel");
                //int mounttiredness = rs.getInt("mounttiredness");

                if (channelserver)
                {
                    MapleMapFactory mapFactory = MasterServer.Instance.ChannelServers[client.ChannelId].MapFactory;
                    ret.Map = mapFactory.GetMap((int)reader["MapId"]);
                    if (ret.Map == null)
                    {
                        //char is on a map that doesn't exist warp it to henesys
                        ret.Map = mapFactory.GetMap(100000000);
                    }
                    else if (ret.Map.ForcedReturnMapId != 999999999)
                    {
                        ret.Map = mapFactory.GetMap(ret.Map.ForcedReturnMapId);
                    }

                    var portal = ret.Map.getPortal(ret.InitialSpawnPoint);
                    if (portal == null)
                    {
                        portal = ret.Map.getPortal(0); // char is on a spawnpoint that doesn't exist - select the first spawnpoint instead
                        ret.InitialSpawnPoint = 0;
                    }
                    ret.Position = portal.Position;

                }

                //if (channelserver)
                //{
                //    int partyid = rs.getInt("party");
                //    if (partyid >= 0)
                //    {
                //        try
                //        {
                //            MapleParty party = client.getChannelServer().getWorldInterface().getParty(partyid);
                //            if (party != null && party.getMemberById(ret.id) != null)
                //            {
                //                ret.party = party;
                //            }
                //        }
                //        catch (RemoteException e)
                //        {
                //            client.getChannelServer().reconnectWorld();
                //        }
                //    }

                //    int messengerid = rs.getInt("messengerid");
                //    int position = rs.getInt("messengerposition");
                //    if (messengerid > 0 && position < 4 && position > -1)
                //    {
                //        try
                //        {
                //            WorldChannelInterface wci = ChannelServer.getInstance(client.getChannel()).getWorldInterface();
                //            MapleMessenger messenger = wci.getMessenger(messengerid);
                //            if (messenger != null)
                //            {
                //                ret.messenger = messenger;
                //                ret.messengerposition = position;
                //            }
                //        }
                //        catch (RemoteException e)
                //        {
                //            client.getChannelServer().reconnectWorld();
                //        }
                //    }
                //}

                //rs.close();
                //ps.close();

                //ps = con.prepareStatement("SELECT * FROM accounts WHERE id = ?");
                //ps.setInt(1, ret.accountid);
                //rs = ps.executeQuery();
                //while (rs.next())
                //{
                //    ret.getClient().setAccountName(rs.getString("name"));
                //    ret.paypalnx = rs.getInt("paypalNX");
                //    ret.maplepoints = rs.getInt("mPoints");
                //    ret.cardnx = rs.getInt("cardNX");
                //    ret.Present = rs.getInt("Present");
                //}
                //rs.close();
                //ps.close();
                reader.Close();

                cmd.CommandText =
                    "SELECT * FROM InventoryItem LEFT JOIN InventoryEquipment ON InventoryItem.Id=InventoryEquipment.InventoryItem_Id WHERE Character_Id=@CharacterId";
                cmd.Parameters.Add(new SqlParameter("@CharacterId", charid));

                if (!channelserver)
                {
                    cmd.CommandText += " AND Type=@Type";
                    cmd.Parameters.Add(new SqlParameter("@Type", MapleInventoryType.Equipped.Value));
                }

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    MapleInventoryType type = MapleInventoryType.GetByType((byte)reader["Type"]);

                    DateTime? expiration;
                    if (reader["ExpireDate"] == DBNull.Value)
                        expiration = null;
                    else
                        expiration = DateTime.Parse((string)reader["ExpireDate"]);

                    if (type == MapleInventoryType.Equip || type == MapleInventoryType.Equipped)
                    {
                        int itemid = (int)reader["ItemId"];
                        Equip equip = new Equip(itemid, (byte)reader["Position"]);

                        if ((bool)reader["IsRing"])
                        {
                            //equip = MapleRing.loadFromDb(itemid, (byte)rs.getInt("position"), rs.getInt("uniqueid"));
                            //log.info("ring loaded");
                        }
                        else
                        {
                            equip.Owner = reader["Owner"] == DBNull.Value ? null : (string) reader["Owner"];
                            equip.Quantity = (short)reader["Quantity"];
                            equip.Acc = (short)reader["Acc"];
                            equip.Avoid = (short)reader["Avoid"];
                            equip.Dex = (short)reader["Dex"];
                            equip.Hands = (short)reader["Hands"];
                            equip.Hp = (short)reader["HP"];
                            equip.Int = (short)reader["Int"];
                            equip.Jump = (short)reader["Jump"];
                            equip.Luk = (short)reader["Luk"];
                            equip.Matk = (short)reader["Matk"];
                            equip.Mdef = (short)reader["Mdef"];
                            equip.Mp = (short)reader["MP"];
                            equip.Speed = (short)reader["Speed"];
                            equip.Str = (short)reader["Str"];
                            equip.Watk = (short)reader["Watk"];
                            equip.Wdef = (short)reader["Wdef"];
                            equip.UpgradeSlots = (byte)reader["UpgradeSlots"];
                            equip.Locked = (byte)reader["Locked"];
                            equip.Level = (byte)reader["Level"];
                            equip.Flag = (byte)reader["Flag"];
                            equip.Vicious = (short)reader["Vicious"];
                            equip.UniqueId = (int)reader["UniqueId"];
                        }
                        if (expiration.HasValue)
                        {
                            if (expiration > DateTime.Now)
                            {
                                equip.Expiration = expiration;
                                ret.Inventorys[type.Value].AddFromDb(equip);
                            }
                            else
                            {
                                string name = MapleItemInformationProvider.Instance.GetName(itemid);
                                ret._mapletips.Add($"现金道具[{name}]由于过期已经被清除了");
                            }
                        }
                        else
                        {
                            ret.Inventorys[type.Value].AddFromDb(equip);
                        }
                    }
                    else
                    {
                        int itemid = (int)reader["ItemId"];
                        Item newitem = new Item(itemid, (byte)reader["Position"], (short)reader["Quantity"]);
                        newitem.Owner = reader["Owner"] == DBNull.Value ? null : (string) reader["Owner"];
                        newitem.UniqueId = (int)reader["UniqueId"];

                        if (expiration.HasValue)
                        {
                            if (expiration > DateTime.Now)
                            {
                                newitem.Expiration = expiration;
                                ret.Inventorys[type.Value].AddFromDb(newitem);
                                if (itemid >= 5000000 && itemid <= 5000100)
                                {
                                    //if (item.items.PetSlot > 0)
                                    //{
                                    //    int index = item.items.PetSlot - 1;
                                    //    // MaplePet pet = MaplePet.loadFromDb(newitem.getItemId(), newitem.getPosition(), newitem.getUniqueId());
                                    //    //Point pos = ret.Position;
                                    //    //pos.Y -= 12;
                                    //    //pet.setPos(pos);
                                    //    //pet.setFh(ret.getMap().getFootholds().findBelow(pet.getPos()).getId());
                                    //    //pet.setStance(0);
                                    //    //int hunger = PetDataFactory.getHunger(pet.getItemId());
                                    //    //if (index > ret.getPets().size())
                                    //    //{
                                    //    //    ret.getPets().add(pet);
                                    //    //    ret.startFullnessSchedule(hunger, pet, ret.getPetSlot(pet));
                                    //    //}
                                    //    //else {
                                    //    //    ret.getPets().add(index, pet);
                                    //    //    ret.startFullnessSchedule(hunger, pet, ret.getPetSlot(pet));
                                    //    //}
                                    //}
                                }
                            }
                            else
                            {
                                string name = MapleItemInformationProvider.Instance.GetName(itemid);
                                ret._mapletips.Add($"现金道具[{name}]由于过期已经被清除了");
                            }
                        }
                        else
                        {
                            ret.Inventorys[type.Value].AddFromDb(newitem);
                        }
                    }
                }
            }

            //if (channelserver)
            //{
            //    ps = con.prepareStatement("SELECT * FROM queststatus WHERE characterid = ?");
            //    ps.setInt(1, charid);
            //    rs = ps.executeQuery();
            //    PreparedStatement pse = con.prepareStatement("SELECT * FROM queststatusmobs WHERE queststatusid = ?");
            //    while (rs.next())
            //    {
            //        MapleQuest q = MapleQuest.getInstance(rs.getInt("quest"));
            //        MapleQuestStatus status = new MapleQuestStatus(q, MapleQuestStatus.Status.getById(rs.getInt("status")));
            //        long cTime = rs.getLong("time");
            //        if (cTime > -1)
            //        {
            //            status.setCompletionTime(cTime * 1000);
            //        }
            //        status.setForfeited(rs.getInt("forfeited"));
            //        ret.quests.put(q, status);
            //        pse.setInt(1, rs.getInt("queststatusid"));
            //        ResultSet rsMobs = pse.executeQuery();
            //        while (rsMobs.next())
            //        {
            //            status.setMobKills(rsMobs.getInt("mob"), rsMobs.getInt("count"));
            //        }
            //        rsMobs.close();
            //    }
            //    rs.close();
            //    ps.close();
            //    pse.close();

            //    ps = con.prepareStatement("SELECT skillid,skilllevel,masterlevel FROM skills WHERE characterid = ?");
            //    ps.setInt(1, charid);
            //    rs = ps.executeQuery();
            //    while (rs.next())
            //    {
            //        ret.skills.put(SkillFactory.getSkill(rs.getInt("skillid")), new SkillEntry(rs.getInt("skilllevel"), rs.getInt("masterlevel")));
            //    }
            //    rs.close();
            //    ps.close();

            //    ps = con.prepareStatement("SELECT * FROM skillmacros WHERE characterid = ?");
            //    ps.setInt(1, charid);
            //    rs = ps.executeQuery();
            //    while (rs.next())
            //    {
            //        int skill1 = rs.getInt("skill1");
            //        int skill2 = rs.getInt("skill2");
            //        int skill3 = rs.getInt("skill3");
            //        String name = rs.getString("name");
            //        int shout = rs.getInt("shout");
            //        int position = rs.getInt("position");
            //        SkillMacro macro = new SkillMacro(skill1, skill2, skill3, name, shout, position);
            //        ret.skillMacros[position] = macro;
            //    }
            //    rs.close();
            //    ps.close();

            //    ps = con.prepareStatement("SELECT `key`,`type`,`action` FROM keymap WHERE characterid = ?");
            //    ps.setInt(1, charid);
            //    rs = ps.executeQuery();
            //    while (rs.next())
            //    {
            //        int key = rs.getInt("key");
            //        int type = rs.getInt("type");
            //        int action = rs.getInt("action");
            //        ret.keymap.put(key, new MapleKeyBinding(type, action));
            //    }
            //    rs.close();
            //    ps.close();

            //    ps = con.prepareStatement("SELECT `locationtype`,`map` FROM savedlocations WHERE characterid = ?");
            //    ps.setInt(1, charid);
            //    rs = ps.executeQuery();
            //    while (rs.next())
            //    {
            //        String locationType = rs.getString("locationtype");
            //        int mapid = rs.getInt("map");
            //        ret.savedLocations[SavedLocationType.valueOf(locationType).ordinal()] = mapid;
            //    }
            //    rs.close();
            //    ps.close();

            //    ps = con.prepareStatement("SELECT `characterid_to`,`when` FROM famelog WHERE characterid = ? AND DATEDIFF(NOW(),`when`) < 30");
            //    ps.setInt(1, charid);
            //    rs = ps.executeQuery();
            //    ret.lastfametime = 0;
            //    ret.lastmonthfameids = new ArrayList<>(31);
            //    while (rs.next())
            //    {
            //        ret.lastfametime = Math.max(ret.lastfametime, rs.getTimestamp("when").getTime());
            //        ret.lastmonthfameids.add(rs.getInt("characterid_to"));
            //    }
            //    rs.close();
            //    ps.close();

            //    ps = con.prepareStatement("SELECT ares_data FROM char_ares_info WHERE charid = ?");
            //    ps.setInt(1, charid);
            //    rs = ps.executeQuery();
            //    while (rs.next())
            //    {
            //        ret.ares_data.add(rs.getString("ares_data"));
            //    }
            //    rs.close();
            //    ps.close();
            //    ret.buddylist.loadFromDb(charid);
            //    ret.storage = MapleStorage.loadOrCreateFromDB(ret.accountid);
            //}

            //String achsql = "SELECT * FROM achievements WHERE accountid = ?";
            //ps = con.prepareStatement(achsql);
            //ps.setInt(1, ret.accountid);
            //rs = ps.executeQuery();
            //while (rs.next())
            //{
            //    ret.finishedAchievements.add(rs.getInt("achievementid"));
            //}
            //rs.close();
            //ps.close();
            //int mountid = ret.getJobType() * 20000000 + 1004;
            //if (ret.getInventory(MapleInventoryType.EQUIPPED).getItem((byte)-18) != null)
            //{
            //    ret.maplemount = new MapleMount(ret, ret.getInventory(MapleInventoryType.EQUIPPED).getItem((byte)-18).getItemId(), mountid);
            //    ret.maplemount.setExp(mountexp);
            //    ret.maplemount.setLevel(mountlevel);
            //    ret.maplemount.setTiredness(mounttiredness);
            //    ret.maplemount.setActive(false);
            //}
            //else {
            //    ret.maplemount = new MapleMount(ret, 0, mountid);
            //    ret.maplemount.setExp(mountexp);
            //    ret.maplemount.setLevel(mountlevel);
            //    ret.maplemount.setTiredness(mounttiredness);
            //    ret.maplemount.setActive(false);
            //}
            //ret.recalcLocalStats();
            //ret.silentEnforceMaxHpMp();
            //ISkill ship = SkillFactory.getSkill(5221006);
            //ret.battleshipHP = (ret.getSkillLevel(ship) * 4000 + (ret.getLevel() - 120) * 2000);
            //ret.loggedInTimer = System.currentTimeMillis();
            return ret;
        }

        public void CancelBuffStats(MapleBuffStat stat)
        {
            List<MapleBuffStat> buffStatList = new List<MapleBuffStat>() { stat };
            DeregisterBuffStats(buffStatList);
            CancelPlayerBuffs(buffStatList);
        }

        private void CancelPlayerBuffs(List<MapleBuffStat> buffstats)
        {
            if (Client.ChannelServer.Characters.FirstOrDefault(x => x.Id == Id) != null)
            { // are we still connected ?
                RecalcLocalStats();
                EnforceMaxHpMp();
                Client.Send(PacketCreator.CancelBuff(buffstats));
                Map.BroadcastMessage(this, PacketCreator.CancelForeignBuff(Id, buffstats), false);
            }
        }

        private void DeregisterBuffStats(List<MapleBuffStat> stats)
        {
            List<MapleBuffStatValueHolder> effectsToCancel = new List<MapleBuffStatValueHolder>(stats.Count);
            foreach (MapleBuffStat stat in stats)
            {
                MapleBuffStatValueHolder mbsvh;
                if (_effects.TryGetValue(stat, out mbsvh))
                {
                    _effects.Remove(stat);
                    bool addMbsvh = true;
                    foreach (var contained in effectsToCancel)
                    {
                        if (mbsvh.StartTime == contained.StartTime && contained.Effect == mbsvh.Effect)
                        {
                            addMbsvh = false;
                        }
                    }
                    if (addMbsvh)
                    {
                        effectsToCancel.Add(mbsvh);
                    }
                    if (stat == MapleBuffStat.Summon || stat == MapleBuffStat.Puppet)
                    {
                        int summonId = mbsvh.Effect.GetSourceId();
                        MapleSummon summon;
                        if (Summons.TryGetValue(summonId, out summon))
                        {
                            Map.BroadcastMessage(PacketCreator.RemoveSpecialMapObject(summon, true));
                            Map.removeMapObject(summon);
                            VisibleMapObjects.Remove(summon);
                            Summons.Remove(summonId);
                        }
                        if (summon.SkillId == 1321007)
                        {
                            //if (beholderHealingSchedule != null)
                            //{
                            //    beholderHealingSchedule.cancel(false);
                            //    beholderHealingSchedule = null;
                            //}
                            //if (beholderBuffSchedule != null)
                            //{
                            //    beholderBuffSchedule.cancel(false);
                            //    beholderBuffSchedule = null;
                            //}
                        }
                    }
                    else if (stat == MapleBuffStat.Dragonblood)
                    {
                        //dragonBloodSchedule.cancel(false);
                        //dragonBloodSchedule = null;
                    }
                }
            }
            foreach (MapleBuffStatValueHolder cancelEffectCancelTasks in effectsToCancel)
            {
                if (!GetBuffStats(cancelEffectCancelTasks.Effect, cancelEffectCancelTasks.StartTime).Any())
                {
                    //cancelEffectCancelTasks.Schedule.cancel(false);
                }
            }
        }

        private List<MapleBuffStat> GetBuffStats(MapleStatEffect effect, long startTime)
        {
            List<MapleBuffStat> stats = new List<MapleBuffStat>();
            foreach (var stateffect in _effects)
            {
                MapleBuffStatValueHolder mbsvh = stateffect.Value;
                if (mbsvh.Effect.SameSource(effect) && (startTime == -1 || startTime == mbsvh.StartTime))
                {
                    stats.Add(stateffect.Key);
                }
            }
            return stats;
        }

        public int? GetBuffedValue(MapleBuffStat effect)
        {
            MapleBuffStatValueHolder mbsvh;
            if (!_effects.TryGetValue(effect, out mbsvh))
            {
                return null;
            }
            return mbsvh.Value;
        }

        public void dispel()
        {
            List<MapleBuffStatValueHolder> allBuffs = new  List<MapleBuffStatValueHolder>(_effects.Values);
            foreach (MapleBuffStatValueHolder mbsvh in allBuffs)
            {
                if (mbsvh.Effect.IsSkill())
                {
                    CancelEffect(mbsvh.Effect, false, mbsvh.StartTime);
                }
            }
        }

        public void giveDebuff(MapleDisease disease, MobSkill skill)
        {
            lock (_diseases)
            {
                if (IsAlive /*&& !isActiveBuffedValue(2321005)*/ && !_diseases.Contains(disease) && _diseases.Count < 2)
                {
                    _diseases.Add(disease);
                    List<Tuple<MapleDisease, int>> debuff = new List<Tuple<MapleDisease, int>>()
                    {
                        (new Tuple<MapleDisease, int>(disease, skill.x))
                    };
                    long mask = debuff.Aggregate<Tuple<MapleDisease, int>, long>(0,
                        (current, statup) => current | (long) statup.Item1);

                    Client.Send(PacketCreator.giveDebuff(mask, debuff, skill));
                    Map.BroadcastMessage(this, PacketCreator.giveForeignDebuff(Id, mask, skill), false);

                    if (IsAlive && _diseases.Contains(disease))
                    {
                        MapleCharacter character = this;
                        MapleDisease disease_ = disease;
                        TimerManager.Instance.ScheduleJob(() =>
                        {
                            if (character._diseases.Contains(disease_))
                            {
                                DispelDebuff(disease_);
                            }
                        }, skill.duration);
                    }
                }
            }
        }

        public void DispelDebuff(MapleDisease debuff)
        {
            if (_diseases.Contains(debuff))
            {
                _diseases.Remove(debuff);
                long mask = (long)debuff;
                //getClient().getSession().write(MaplePacketCreator.cancelDebuff(mask));
                //getMap().broadcastMessage(this, MaplePacketCreator.cancelForeignDebuff(id, mask), false);
            }
        }

        public void DispelDebuffs()
        {
            List<MapleDisease> ret = new List<MapleDisease>();
            foreach (MapleDisease disease in ret)
            {
                if (disease != MapleDisease.Seduce && disease != MapleDisease.Stun)
                {
                    _diseases.Remove(disease);
                    long mask = (long)disease;
                    //getClient().getSession().write(MaplePacketCreator.cancelDebuff(mask));
                    //getMap().broadcastMessage(this, MaplePacketCreator.cancelForeignDebuff(id, mask), false);
                }
            }
        }

        public void DispelDebuffsi()
        {
            List<MapleDisease> ret = new List<MapleDisease>();
            foreach (MapleDisease disease in ret)
            {
                if (disease != MapleDisease.Seal)
                {
                    _diseases.Remove(disease);
                    long mask = (long)disease;
                    //getClient().getSession().write(MaplePacketCreator.cancelDebuff(mask));
                    //getMap().broadcastMessage(this, MaplePacketCreator.cancelForeignDebuff(id, mask), false);
                }
            }
        }

        public void DispelSkill(int skillid)
        {
            LinkedList<MapleBuffStatValueHolder> allBuffs = new LinkedList<MapleBuffStatValueHolder>(_effects.Values);
            foreach (MapleBuffStatValueHolder mbsvh in allBuffs)
            {
                if (skillid == 0)
                {
                    if (mbsvh.Effect.IsSkill() && (mbsvh.Effect.GetSourceId() % 20000000 == 1004 || DispelSkills(mbsvh.Effect.GetSourceId())))
                    {
                        //cancelEffect(mbsvh.Effect, false, mbsvh.StartTime);
                    }
                }
                else if (mbsvh.Effect.IsSkill() && mbsvh.Effect.GetSourceId() == skillid)
                {
                    // cancelEffect(mbsvh.Effect, false, mbsvh.StartTime);
                }
            }
        }

        private bool DispelSkills(int skillid)
        {
            switch (skillid)
            {
                case 1004:
                case 1321007:
                case 2121005:
                case 2221005:
                case 2311006:
                case 2321003:
                case 3111002:
                case 3111005:
                case 3211002:
                case 3211005:
                case 4111002:
                case 11001004:
                case 12001004:
                case 13001004:
                case 14001005:
                case 15001004:
                case 12111004:
                case 20001004:
                    return true;
                default:
                    return false;
            }
        }

        public void CancelAllDebuffs()
        {
            List<MapleDisease> ret = new List<MapleDisease>();
            foreach (MapleDisease disease in ret)
            {
                _diseases.Remove(disease);
                long mask = (long)disease;
                //getClient().getSession().write(MaplePacketCreator.cancelDebuff(mask));
                //getMap().broadcastMessage(this, MaplePacketCreator.cancelForeignDebuff(id, mask), false);
            }
        }

        public int getSkillLevel(int skill)
        {
            SkillEntry ret;
            if (!Skills.TryGetValue(SkillFactory.GetSkill(skill), out ret))
            {
                return 0;
            }
            return ret.SkilLevel;
        }

        public int getSkillLevel(ISkill skill)
        {
            SkillEntry ret;

            if (skill != null && (skill.SkillId == 1009 || skill.SkillId == 10001009 || skill.SkillId == 1010 || skill.SkillId == 1011 || skill.SkillId == 10001010 || skill.SkillId == 10001011))
            {
                return 1;
            }
            else if (!Skills.TryGetValue(skill, out ret))
            {
                return 0;
            }
            return ret.SkilLevel;
        }

        public void changeMap(MapleMap to) => changeMap(to, to.getPortal(0));

        public void changeMap(MapleMap to, IMaplePortal pto)
        {
            if (to.MapId == 100000200 || to.MapId == 211000100 || to.MapId == 220000300)
            {
                ChangeMapInternal(to, pto.Position, PacketCreator.getWarpToMap(to, (byte)(pto.PortalId - 2), this));
            }
            else {
                ChangeMapInternal(to, pto.Position, PacketCreator.getWarpToMap(to, pto.PortalId, this));
            }
        }

        public void changeMap(MapleMap to, Point pos)
        {
            ChangeMapInternal(to, pos, PacketCreator.getWarpToMap(to, 0x80, this));
        }

        private void ChangeMapInternal(MapleMap to, Point pos, OutPacket warpPacket)
        {

            Client.Send(warpPacket);

            //IPlayerInteractionManager interaction = MapleCharacter.this.getInteraction();
            //if (interaction != null)
            //{
            //    if (interaction.isOwner(MapleCharacter.this))
            //    {
            //        if (interaction.getShopType() == 2)
            //        {
            //            interaction.removeAllVisitors(3, 1);
            //            interaction.closeShop(((MaplePlayerShop)interaction).returnItems(getClient()));
            //        }
            //        else if (interaction.getShopType() == 1)
            //        {
            //            getClient().getSession().write(MaplePacketCreator.shopVisitorLeave(0));
            //            if (interaction.getItems().size() == 0)
            //            {
            //                interaction.removeAllVisitors(3, 1);
            //                interaction.closeShop(((HiredMerchant)interaction).returnItems(getClient()));
            //            }
            //        }
            //        else if (interaction.getShopType() == 3 || interaction.getShopType() == 4)
            //        {
            //            interaction.removeAllVisitors(3, 1);
            //        }
            //    }
            //    else {
            //        interaction.removeVisitor(MapleCharacter.this);
            //    }
            //}
            //MapleCharacter.this.setInteraction(null);

            Map.RemovePlayer(this);
            if (Client.ChannelServer.Characters.Contains(this))
            {
                Map = to;
                Position = pos;
                to.AddPlayer(this);
                //lastPortalPoint = getPosition();
                //if (Party != null)
                //{
                //    silentPartyUpdate();
                //    getClient().getSession().write(MaplePacketCreator.updateParty(getClient().getChannel(), party, PartyOperation.SILENT_UPDATE, null));
                //    updatePartyMemberHP();
                //}
                //if (getMap().getHPDec() > 0)
                //{
                //    hpDecreaseTask = TimerManager.Instance.Schedule(() =>
                //    {
                //        doHurtHp();
                //    }, 10000);
                //}
                //if (to.MapID == 980000301) { //todo: all CPq map id's
                //    setTeam(rand(0, 1));
                //    Client.Send(PacketCreator.startMonsterCarnival(getTeam()));
                //}
            }


        }


        public int GetItemAmount(int itemid)
        {
            MapleInventoryType type = MapleItemInformationProvider.Instance.GetInventoryType(itemid);
            MapleInventory iv = Inventorys[type.Value];
            return iv.CountById(itemid);
        }

        public override void SendDestroyData(MapleClient client)
        {
            client.Send(PacketCreator.RemovePlayerFromMap(ObjectId));
        }

        public override void SendSpawnData(MapleClient client)
        {
            if ((IsHidden && client.Character.GmLevel > 0) || !IsHidden)
            {
                client.Send(PacketCreator.SpawnPlayerMapobject(this));
                foreach (MaplePet pet in Pets)
                {
                    Map.BroadcastMessage(this, PacketCreator.ShowPet(this, pet, false, false), false);
                }
            }
        }

        public override MapleMapObjectType GetType() => MapleMapObjectType.Player;


        public void GainMeso(int gain, bool show, bool enableActions)
        {
            GainMeso(gain, show, enableActions, false);
        }

        public void GainMeso(int gain, bool show, bool enableActions = false, bool inChat = false)
        {
            if (Money.Value + gain < 0)
            {
                Client.Send(PacketCreator.EnableActions());
                return;
            }
            int newVal = Money.Add(gain);
            //updateSingleStat(MapleStat.MESO, newVal, enableActions);
            if (show)
            {
                Client.Send(PacketCreator.GetShowMesoGain(gain, inChat));
            }
        }

        public void AddCooldown(int skillId, long startTime, long length, string jobname)
        {
            if (GmLevel < 5)
            {
                if (_coolDowns.ContainsKey(skillId))
                {
                    _coolDowns.Remove(skillId);
                }
                _coolDowns.Add(skillId, new MapleCoolDownValueHolder(skillId, startTime, length, jobname));
            }
            else {
                Client.Send(PacketCreator.SkillCooldown(skillId, 0));
            }
        }

        public void GiveCoolDowns(int skillid, long starttime, long length)
        {
            int time = (int)(length + starttime - DateTime.Now.GetTimeMilliseconds());
            string jobname = TimerManager.Instance.ScheduleJob(() => _cancelCooldownAction(this, skillid), time);
            AddCooldown(skillid, DateTime.Now.GetTimeMilliseconds(), time, jobname);
        }

        public void RemoveCooldown(int skillId)
        {
            if (_coolDowns.ContainsKey(skillId))
            {
                _coolDowns.Remove(skillId);
            }
            Client.Send(PacketCreator.SkillCooldown(skillId, 0));
        }

        public bool SkillisCooling(int skillId)
        {
            return _coolDowns.ContainsKey(skillId);
        }

        public List<PlayerCoolDownValueHolder> GetAllCooldowns()
        {
            List<PlayerCoolDownValueHolder> ret = new List<PlayerCoolDownValueHolder>();
            foreach (MapleCoolDownValueHolder mcdvh in _coolDowns.Values)
            {
                ret.Add(new PlayerCoolDownValueHolder(mcdvh.SkillId, mcdvh.StartTime, mcdvh.Duration));
            }
            return ret;
        }

        public List<int> GetTRockMaps(int type)
        {
            List<int> rockmaps = new List<int>();

            //using (var db = new NeoDatabase())
            //{
            //    var result = db.RockLocations.Where(x => x.CharacterId == Id && x.Type == type).Select(x => x.MapId).Take(type == 1 ? 10 : 5);
            //    foreach (var mapid in result)
            //    {
            //        rockmaps.Add(mapid);
            //    }
            //    return rockmaps;
            //}
            return rockmaps;
        }

        public void DropMessage(string message)
        {
            Client.Send(PacketCreator.ServerNotice(GmLevel > 0 ? PacketCreator.ServerMessageType.LightBlueText : PacketCreator.ServerMessageType.PinkText, message));
        }

        public void DropMessage(PacketCreator.ServerMessageType type, string message)
        {
            Client.Send(PacketCreator.ServerNotice(type, message));
        }

        public MapleQuestStatus GetQuest(MapleQuest quest)
        {
            if (!_quests.ContainsKey(quest))
            {
                return new MapleQuestStatus(quest, MapleQuestStatusType.NotStarted);
            }
            return _quests[quest];
        }

        public int GetNumQuest()
        {
            int i = 0;
            foreach (MapleQuestStatus q in _quests.Values)
            {
                if (q.Status == MapleQuestStatusType.Completed)
                {
                    i++;
                }
            }
            return i;
        }
        public void gainExp(int gain, bool show, bool inChat)
        {
            gainExp(gain, show, inChat, true);
        }
        public void gainExp(int gain, bool show, bool inChat, bool white)
        {
            if ((Level < 200 && (Math.Floor(Job.JobId / 100D) < 10 || Math.Floor(Job.JobId / 1000D) == 2)) || Level < 180)
            {
                if (Exp.Value + gain >= ExpTable.GetExpNeededForLevel(Level + 1))
                {
                    Exp.Add(gain);
                    LevelUp();
                    if (Exp.Value > ExpTable.GetExpNeededForLevel(Level + 1))
                    {
                        Exp.Exchange(ExpTable.GetExpNeededForLevel(Level + 1));
                    }
                }
                else {
                    Exp.Add(gain);
                }
            }
            else if (Exp.Value != 0)
            {
                Exp.Reset();
            }
            /**
          * @112208 - 精灵吊坠
          * @112207 - 温暖的围脖
          * @以上装备打怪可以多获得2倍的经验
          */
            UpdateSingleStat(MapleStat.Exp, Exp.Value);
            if (show && gain != 0)
            {
                var eqp = Inventorys[MapleInventoryType.Equipped.Value].Inventory[17];
                if ((eqp != null && eqp.ItemId == 1122018) || (eqp != null && eqp.ItemId == 1122017))
                {
                    //围脖吊坠
                    if (Level >= 1)
                    {
                        gain *= 2; //**经验倍数X2
                    }
                    Client.Send(PacketCreator.GetShowExpGain(gain, inChat, white, 1));
                }
                else {
                    Client.Send(PacketCreator.GetShowExpGain(gain, inChat, white));
                }
            }
        }

        private static short Rand(int lbound, int ubound) => (short)(Randomizer.NextDouble() * (ubound - lbound + 1) + lbound);

        public void LevelUp()
        {
            ISkill improvingMaxHp = null;
            int improvingMaxHpLevel = 0;

            ISkill improvingMaxMp = null;
            int improvingMaxMpLevel = 0;

            if (Job.JobId >= 1000 && Job.JobId <= 1511 && Level < 70)
            {
                RemainingAp += 1;
            }
            RemainingAp += 5;

            if (Job == MapleJob.Ares || Job == MapleJob.Ares1 || Job == MapleJob.Ares2 || Job == MapleJob.Ares3 || Job == MapleJob.Ares4)
            {
                MaxHp += (short)Rand(24, 28);
                MaxMp += (short)Rand(4, 6);
            }
            if (Job == MapleJob.Beginner || Job == MapleJob.Knight)
            {
                MaxHp += (short)Rand(12, 16);
                MaxMp += (short)Rand(10, 12);
            }
            else if (Job == MapleJob.Warrior || Job == MapleJob.GhostKnight || Job == MapleJob.Ares1)
            {
                improvingMaxHp = SkillFactory.GetSkill(1000001);
                improvingMaxHpLevel = getSkillLevel(improvingMaxHp);
                if (Job == MapleJob.GhostKnight)
                {
                    improvingMaxHp = SkillFactory.GetSkill(11000000);
                    improvingMaxHpLevel = getSkillLevel(improvingMaxHp);
                }
                MaxHp += Rand(24, 28);
                MaxMp += Rand(4, 6);
            }
            else if (Job == MapleJob.Magician || Job == MapleJob.FireKnight)
            {
                improvingMaxMp = SkillFactory.GetSkill(2000001);
                improvingMaxMpLevel = getSkillLevel(improvingMaxMp);
                if (Job == MapleJob.FireKnight)
                {
                    improvingMaxMp = SkillFactory.GetSkill(12000000);
                    improvingMaxMpLevel = getSkillLevel(improvingMaxMp);
                }
                MaxHp += Rand(10, 14);
                MaxMp += Rand(22, 24);
            }
            else if (Job == MapleJob.Bowman || Job == MapleJob.Thief || Job == MapleJob.Gm || Job == MapleJob.WindKnight || Job == MapleJob.NightKnight)
            {
                MaxHp += Rand(20, 24);
                MaxMp += Rand(14, 16);
            }
            else if (Job == MapleJob.Pirate || Job == MapleJob.ThiefKnight)
            {
                improvingMaxHp = SkillFactory.GetSkill(5100000);
                improvingMaxHpLevel = getSkillLevel(improvingMaxHp);
                if (Job == MapleJob.GhostKnight)
                {
                    improvingMaxHp = SkillFactory.GetSkill(15100000);
                    improvingMaxHpLevel = getSkillLevel(improvingMaxHp);
                }
                MaxHp += Rand(22, 26);
                MaxMp += Rand(18, 23);
            }
            if (improvingMaxHpLevel > 0)
            {
                MaxHp += (short)improvingMaxHp.GetEffect(improvingMaxHpLevel).X;
            }
            if (improvingMaxMpLevel > 0)
            {
                MaxMp += (short)improvingMaxMp.GetEffect(improvingMaxMpLevel).X ;
            }

            MaxMp += (short)(Localint / 10);
            Exp.Add(-ExpTable.GetExpNeededForLevel(Level + 1));
            Level++;

            DropMessage($"[系统信息] 升级了！恭喜你升到 { Level } 级!");
            if (Exp.Value > 0)
            {
                Exp.Reset();
            }
            if (Level == 200)
            {
                Exp.Reset();
                var packet = PacketCreator.ServerNotice(0, $"祝贺 { Name } 到达200级！");
                try
                {
                    // Client.ChannelServer.getWorldInterface().broadcastMessage(CharacterName, packet);
                }
                catch
                {
                    //getClient().getChannelServer().reconnectWorld();
                }
            }
            if (Hp <= 70)
            {
                DropMessage($"[警告] 注意!!你的生命值为 { Hp } 已不足 100 !");
            }

            MaxHp = Math.Min((short)30000, MaxHp);
            MaxMp = Math.Min((short)30000, MaxMp);

            List<Tuple<MapleStat, int>> statup = new List<Tuple<MapleStat, int>>(8);
            statup.Add(new Tuple<MapleStat, int>(MapleStat.Availableap, RemainingAp));
            statup.Add(new Tuple<MapleStat, int>(MapleStat.Maxhp, MaxHp));
            statup.Add(new Tuple<MapleStat, int>(MapleStat.Maxmp, MaxMp));
            statup.Add(new Tuple<MapleStat, int>(MapleStat.Hp, MaxHp));
            statup.Add(new Tuple<MapleStat, int>(MapleStat.Mp, MaxMp));
            statup.Add(new Tuple<MapleStat, int>(MapleStat.Exp, Exp.Value));
            statup.Add(new Tuple<MapleStat, int>(MapleStat.Level, Level));

            if (Job != MapleJob.Beginner)
            {
                RemainingSp += 3;
                statup.Add(new Tuple<MapleStat, int>(MapleStat.Availablesp, RemainingSp));
            }

            Hp = MaxHp;
            Mp = MaxMp;

            if (Job == MapleJob.GhostKnight && Level <= 10)
            {
                Client.Send(PacketCreator.ServerNotice(PacketCreator.ServerMessageType.PinkText, "骑士团职业已经需求"));
            }

            if (Level >= 30)
            {
                //finishAchievement(3);
            }
            if (Level >= 70)
            {
                // finishAchievement(4);
            }
            if (Level >= 120)
            {
                //finishAchievement(5);
            }
            if (Level == 200)
            {
                //finishAchievement(22);
            }

            /*@升级送点
             *@10.30.70.120.200可以送
             *@全程为7000点卷.
             */

            if (Level == 30)
            {
                GainNexonPoint(1000);
                DropMessage($"恭喜您等级达到 { Level } 级!得到了{1000}点卷奖励!");
            }
            if (Level == 70)
            {
                GainNexonPoint(1500);
                DropMessage($"恭喜您等级达到 {Level } 级!得到了{1500}点卷奖励!");
            }
            if (Level == 120)
            {
                GainNexonPoint(2000);
                DropMessage($"恭喜您等级达到 { Level } 级!得到了{2000}点卷奖励!");
            }
            if (Level == 200)
            {
                GainNexonPoint(2500);
                DropMessage($"恭喜您等级达到 { Level } 级!得到了{2500}点卷奖励!");
            }


            Client.Send(PacketCreator.UpdatePlayerStats(statup));
            Map.BroadcastMessage(this, PacketCreator.ShowLevelup(Id), false);
            RecalcLocalStats();
            SilentPartyUpdate();
            GuildUpdate();
        }

        public void ChangeSkillLevel(ISkill skill, int newLevel, int newMasterlevel)
        {
            Skills.Add(skill, new SkillEntry(newLevel, newMasterlevel));
            Client.Send(PacketCreator.UpdateSkill(skill.SkillId, newLevel, newMasterlevel));
        }

        public void UpdateQuest(MapleQuestStatus quest)
        {
            _quests.Add(quest.Quest, quest);
            int questId = quest.Quest.GetQuestId();
            if (questId == 4760 || questId == 4761 || questId == 4762 || questId == 4763 || questId == 4764 || questId == 4765 || questId == 4766 || questId == 4767 || questId == 4768 || questId == 4769 || questId == 4770 || questId == 4771)
            {
                Client.Send(PacketCreator.CompleteQuest(this, (short)questId));
                Client.Send(PacketCreator.UpdateQuestInfo(this, (short)questId, quest.Npcid, 8));
            }
            else
            {
                if (quest.Status == MapleQuestStatusType.Started)
                {
                    Client.Send(PacketCreator.StartQuest(this, (short)questId));
                    Client.Send(PacketCreator.UpdateQuestInfo(this, (short)questId, quest.Npcid, 8));
                }
                else if (quest.Status == MapleQuestStatusType.Completed)
                {
                    Client.Send(PacketCreator.CompleteQuest(this, (short)questId));
                }
                else if (quest.Status == MapleQuestStatusType.NotStarted)
                {
                    Client.Send(PacketCreator.ForfeitQuest(this, (short)questId));
                }
            }
        }

        public List<MapleQuestStatus> GetStartedQuests()
        {
            List<MapleQuestStatus> ret = new List<MapleQuestStatus>();
            foreach (var questStatus in _quests.Values)
            {
                if (questStatus.Status == MapleQuestStatusType.Completed)
                {
                    continue;
                }
                else if (questStatus.Status == MapleQuestStatusType.Started)
                {
                    ret.Add(questStatus);
                }
            }
            return ret;
        }

        public List<MapleQuestStatus> GetCompletedQuests()
        {
            List<MapleQuestStatus> ret = new List<MapleQuestStatus>();
            foreach (var questStatus in _quests.Values)
            {
                if (questStatus.Status == MapleQuestStatusType.Completed)
                {
                    ret.Add(questStatus);
                }
            }
            return ret;
        }

        public void UpdateSingleStat(MapleStat stat, int newval, bool itemReaction = false)
        {
            Tuple<MapleStat, int> statpair = new Tuple<MapleStat, int>(stat, newval);
            Client.Send(PacketCreator.UpdatePlayerStats(new List<Tuple<MapleStat, int>>() { statpair }, itemReaction));
        }

        public void SilentPartyUpdate()
        {
            //if (Party != null)
            //{
            //    try
            //    {
            //        getClient().getChannelServer().getWorldInterface().updateParty(Party.PartyID, PartyOperation.SILENT_UPDATE, new MaplePartyCharacter(MapleCharacter.this));
            //    }
            //    catch
            //    {
            //        log.error("REMOTE THROW", e);
            //        getClient().getChannelServer().reconnectWorld();
            //    }
            //}
        }

        public void GuildUpdate()
        {
            //if (this.guildid <= 0)
            //{
            //    return;
            //}

            //mgc.setLevel(this.level);
            //mgc.setJobId(this.job.getId());

            //try
            //{
            //    this.client.getChannelServer().getWorldInterface().memberLevelJobUpdate(this.mgc);
            //}
            //catch (RemoteException re)
            //{
            //    log.error("RemoteExcept while trying to update level/job in guild.", re);
            //}
        }

        public void SendMacros()
        {
            bool macros = false;
            for (int i = 0; i < 5; i++)
            {
                if (_skillMacros[i] != null)
                {
                    macros = true;
                }
            }
            if (macros)
            {
                Client.Send(PacketCreator.GetMacros(_skillMacros));
            }
        }

        public void ShowNote()
        {
            //Connection con = DatabaseConnection.getConnection();
            //PreparedStatement ps = con.prepareStatement("SELECT * FROM notes WHERE `to`=?", ResultSet.TYPE_SCROLL_SENSITIVE, ResultSet.CONCUR_UPDATABLE);
            //ps.setString(1, this.getName());
            //ResultSet rs = ps.executeQuery();

            //rs.last();
            //int count = rs.getRow();
            //rs.first();
            Client.Send(PacketCreator.ShowNotes(0));
            //ps.close();
        }

        public int GetPetSlot(MaplePet pet)
        {
            if (Pets.Any())
            {
                for (int i = 0; i < Pets.Count; i++)
                {
                    if (Pets[i] != null)
                    {
                        if (Pets[i].UniqueId == pet.UniqueId)
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        public MapleStatEffect GetStatForBuff(MapleBuffStat effect)
        {
            MapleBuffStatValueHolder mbsvh;
            return !_effects.TryGetValue(effect, out mbsvh) ? null : mbsvh.Effect;
        }

        public void UpdatePartyMemberHp()
        {
            if (Party != null)
            {
                int channel = Client.ChannelId;
                Party.GetMembers().ForEach(partychr =>
                {
                    if (partychr.MapId == Map.MapId && partychr.ChannelId == channel)
                    {
                        MapleCharacter other = MasterServer.Instance.ChannelServers[channel].Characters.FirstOrDefault(x => x.Name == partychr.CharacterName);
                        other?.Client.Send(PacketCreator.UpdatePartyMemberHp(Id, Hp, Localmaxhp));
                    }
                });
            }
        }

        public void ReceivePartyMemberHp()
        {
            if (Party != null)
            {
                int channel = Client.ChannelId;
                Party.GetMembers().ForEach(partychr =>
                {
                    if (partychr.MapId == Map.MapId && partychr.ChannelId == channel)
                    {
                        MapleCharacter other = MasterServer.Instance.ChannelServers[channel].Characters.FirstOrDefault(x => x.Name == partychr.CharacterName);
                        if (other != null)
                        {
                            Client.Send(PacketCreator.UpdatePartyMemberHp(other.Id, other.Hp, other.Localmaxhp));
                        }
                    }
                });
            }
        }


        public void LeaveMap()
        {
            _controlled.Clear();
            VisibleMapObjects.Clear();
            if (Chair != 0)
            {
                Chair = 0;
            }
            //if (hpDecreaseTask != null)
            //{
            //    hpDecreaseTask.cancel(false);
            //}
        }

        public void SetMp(short newmp)
        {
            short tmp = newmp;
            if (tmp < 0)
            {
                tmp = 0;
            }
            if (tmp > Localmaxmp)
            {
                tmp = Localmaxmp;
            }
            Mp = tmp;
        }

        public void SetHp(short newhp, bool silent = false)
        {
            short oldHp = Hp;
            short thp = newhp;
            if (thp < 0)
            {
                thp = 0;
            }
            if (thp > Localmaxhp)
            {
                thp = Localmaxhp;
            }
            Hp = thp;

            if (!silent)
            {
                UpdatePartyMemberHp();
            }
            if (oldHp > Hp && !IsAlive)
            {
                PlayerDead();
            }

            CheckBerserk();
        }

        public void CheckBerserk()
        {
            //if (BerserkSchedule != null)
            //{
            //    BerserkSchedule.cancel(false);
            //}

            MapleCharacter chr = this;
            ISkill berserkX = SkillFactory.GetSkill(1320006);
            int skilllevel = getSkillLevel(berserkX);
            if (chr.Job == MapleJob.Darkknight && skilllevel >= 1)
            {
                MapleStatEffect ampStat = berserkX.GetEffect(skilllevel);
                int x = ampStat.X;
                int hp = chr.Hp;
                int mhp = chr.MaxHp;
                int ratio = hp * 100 / mhp;
                if (ratio > x)
                {
                    //Berserk = false;
                }
                else {
                    //Berserk = true;
                }

                //            BerserkSchedule = TimerManager.getInstance().register(new Runnable() {

                //            @Override
                //            public void run()
                //    {
                //        //getClient().getSession().write(MaplePacketCreator.showOwnBerserk(skilllevel, Berserk));
                //        //getMap().broadcastMessage(MapleCharacter.this, MaplePacketCreator.showBerserk(getId(), skilllevel, Berserk), false);
                //    }
                //}, 5000, 3000);
            }
        }

        private void PlayerDead()
        {
            //if (getEventInstance() != null)
            //{
            //    getEventInstance().playerKilled(this);
            //}

            //dispelSkill(0);
            //cancelAllDebuffs();
            //cancelMorphs();

            //int[] charmID = { 5130000, 5130002, 5131000, 4031283, 4140903 };
            //MapleCharacter player = Client.Character;
            //int possesed = 0;
            //int i;

            ////Check for charms
            //for (i = 0; i < charmID.Length; i++)
            //{
            //    int quantity = getItemQuantity(charmID[i], false);
            //    if (possesed == 0 && quantity > 0)
            //    {
            //        possesed = quantity;
            //        break;
            //    }
            //}

            //if (possesed > 0 && !getMap().hasEvent())
            //{
            //    possesed -= 1;
            //    getClient().getSession().write(MaplePacketCreator.serverNotice(5, "��ʹ���� [�����] ���������ľ��鲻����٣�ʣ�� (" + possesed + " ��)"));
            //    MapleInventoryManipulator.removeById(getClient(), MapleItemInformationProvider.getInstance().getInventoryType(charmID[i]), charmID[i], 1, true, false);
            //}
            //else if (getMap().hasEvent())
            //{
            //    getClient().getSession().write(MaplePacketCreator.serverNotice(5, "�������ͼ�����������ľ���ֵ������١�"));
            //}
            //else if (player.getJob() != MapleJob.BEGINNER || player.getJob() != MapleJob.KNIGHT || player.getJob() != MapleJob.Ares)
            //{
            //    //Lose XP
            //    int XPdummy = ExpTable.getExpNeededForLevel(player.getLevel() + 1);
            //    if (player.getMap().isTown())
            //    {
            //        XPdummy *= 0.01;
            //    }
            //    if (XPdummy == ExpTable.getExpNeededForLevel(player.getLevel() + 1))
            //    {
            //        if (player.getLuk() <= 100 && player.getLuk() > 8)
            //        {
            //            XPdummy *= 0.10 - (player.getLuk() * 0.0005);
            //        }
            //        else if (player.getLuk() < 8)
            //        {
            //            XPdummy *= 0.10; //Otherwise they lose about 9 percent
            //        }
            //        else {
            //            XPdummy *= 0.10 - (100 * 0.0005);
            //        }
            //    }
            //    if ((player.getExp() - XPdummy) > 0)
            //    {
            //        player.gainExp(-XPdummy, false, false);
            //    }
            //    else {
            //        player.gainExp(-player.getExp(), false, false);
            //    }
            //}
            //if (getBuffedValue(MapleBuffStat.MORPH) != null)
            //{
            //    cancelEffectFromBuffStat(MapleBuffStat.MORPH);
            //}
            //if (getBuffedValue(MapleBuffStat.MONSTER_RIDING) != null)
            //{
            //    cancelEffectFromBuffStat(MapleBuffStat.MONSTER_RIDING);
            //}
            //client.getSession().write(PacketCreator.EnableActions());
        }

        private void EnforceMaxHpMp()
        {
            List<Tuple<MapleStat, int>> stats = new List<Tuple<MapleStat, int>>(2);
            if (Mp > Localmaxmp)
            {
                SetMp(Mp);
                stats.Add(new Tuple<MapleStat, int>(MapleStat.Mp, Mp));
            }
            if (Hp > Localmaxhp)
            {
                SetHp(Hp);
                stats.Add(new Tuple<MapleStat, int>(MapleStat.Hp, Hp));
            }
            if (stats.Any())
            {
                Client.Send(PacketCreator.UpdatePlayerStats(stats));
            }
        }

        public void CancelAllBuffs()
        {
            List<MapleBuffStatValueHolder> allBuffs = new List<MapleBuffStatValueHolder>(_effects.Count);
            foreach (MapleBuffStatValueHolder mbsvh in allBuffs)
            {
                CancelEffect(mbsvh.Effect, false, mbsvh.StartTime);
            }
        }

        public void CancelEffect(MapleStatEffect effect, bool overwrite, long startTime)
        {
            List<MapleBuffStat> buffstats;
            if (!overwrite)
            {
                buffstats = GetBuffStats(effect, startTime);
            }
            else {
                List<Tuple<MapleBuffStat, int>> statups = effect.GetStatups();
                buffstats = new List<MapleBuffStat>(statups.Count);
                foreach (var statup in statups)
                {
                    buffstats.Add(statup.Item1);
                }
            }
            DeregisterBuffStats(buffstats);
            if (effect.IsMagicDoor())
            {
                // remove for all on maps
                if (Doors.Any())
                {
                    Doors.GetEnumerator().MoveNext();
                    MapleDoor door = Doors.GetEnumerator().Current;
                    foreach (MapleCharacter chr in door.TargetMap.Characters)
                    {
                        door.SendDestroyData(chr.Client);
                    }
                    foreach (MapleCharacter chr in door.Town.Characters)
                    {
                        door.SendDestroyData(chr.Client);
                    }
                    foreach (MapleDoor destroyDoor in Doors)
                    {
                        door.TargetMap.removeMapObject(destroyDoor);
                        door.Town.removeMapObject(destroyDoor);
                    }
                    Doors.Clear();
                    SilentPartyUpdate();
                }
            }
            if (!overwrite)
            {
                CancelPlayerBuffs(buffstats);
                if (effect.IsHide() && Map.Mapobjects.ContainsKey(ObjectId))
                {
                    this.IsHidden = false;
                    Map.BroadcastMessage(this, PacketCreator.SpawnPlayerMapobject(this), false);
                    foreach (MaplePet pett in Pets)
                    {
                        Map.BroadcastMessage(this, PacketCreator.ShowPet(this, pett, false, false), false);
                    }
                }
            }
        }

        public bool haveItem(int itemid, int quantity, bool checkEquipped, bool exact)
        {
            // if exact is true, then possessed must be EXACTLY equal to quantity. else, possessed can be >= quantity
            MapleInventoryType type = MapleItemInformationProvider.Instance.GetInventoryType(itemid);
            MapleInventory iv = Inventorys[type.Value];
            int possessed = iv.CountById(itemid);
            if (checkEquipped)
            {
                possessed += Inventorys[MapleInventoryType.Equipped.Value].CountById(itemid);
            }
            if (exact)
            {
                return possessed == quantity;
            }
            else {
                return possessed >= quantity;
            }
        }

        public bool haveItem(int[] itemids, int quantity, bool exact)
        {
            foreach (int itemid in itemids)
            {
                MapleInventoryType type = MapleItemInformationProvider.Instance.GetInventoryType(itemid);
                MapleInventory iv = Inventorys[type.Value];
                int possessed = iv.CountById(itemid);
                possessed += Inventorys[MapleInventoryType.Equipped.Value].CountById(itemid);
                if (possessed >= quantity)
                {
                    if (exact)
                    {
                        if (possessed == quantity)
                        {
                            return true;
                        }
                    }
                    else {
                        return true;
                    }
                }
            }
            return false;
        }

        public void controlMonster(MapleMonster monster, bool aggro)
        {
            monster.SetController(this);
            _controlled.Add(monster);
            Client.Send(PacketCreator.controlMonster(monster, false, aggro));
        }

        public void stopControllingMonster(MapleMonster monster)
        {
            _controlled.Remove(monster);
        }

        public List<MapleMonster> getControlledMonsters()
        {
            return _controlled;
        }

     

        readonly Action<MapleCharacter, int> _cancelCooldownAction = (target, skillId) =>
                 {
                     MapleCharacter realTarget = (MapleCharacter)new WeakReference(target).Target;
                     realTarget?.RemoveCooldown(skillId);
                 };
    }

    struct MapleBuffStatValueHolder
    {

        public MapleStatEffect Effect { get; private set; }
        public long StartTime { get; private set; }
        public int Value { get; private set; }
        public Action Schedule { get; private set; }

        public MapleBuffStatValueHolder(MapleStatEffect effect, long startTime, Action schedule, int value)
        {
            Effect = effect;
            StartTime = startTime;
            Schedule = schedule;
            Value = value;
        }
    }
}