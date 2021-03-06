﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoMapleStory.Core.Database.Models
{
    [Table("InventoryEquipments")]
    public class InventoryEquipmentModel
    {
        [Key]
        public Guid Id { get; set; }

        public byte UpgradeSlots { get; set; }

        public byte Locked { get; set; }

        public byte Level { get; set; }

        public short Str { get; set; }

        public short Dex { get; set; }

        public short Int { get; set; }

        public short Luk { get; set; }

        public short Hp { get; set; }

        public short Mp { get; set; }

        public short Watk { get; set; }

        public short Matk { get; set; }

        public short Wdef { get; set; }

        public short Mdef { get; set; }

        public short Acc { get; set; }

        public short Avoid { get; set; }

        public short Hands { get; set; }

        public short Speed { get; set; }

        public short Jump { get; set; }

        public short Vicious { get; set; }

        public bool IsRing { get; set; }

        public byte Flag { get; set; }

        public virtual InventoryItemModel InventoryItem { get; set; }
    }
}