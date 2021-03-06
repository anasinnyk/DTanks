﻿using Networking.Msg;
using PlayerData;
using UnityEngine;

namespace GameServer.Commands
{
    public class ServerBattleItemDroppingCommand : ServerBattleCommandBase
    {
        private float kMinDropInterval = 5f;
        private float kMaxDropInterval = 15f;
        private int kMaxItemsInField = 10;
        
        private int _lastDropItemIndex = 0;

        public override void Start()
        {
            base.Start();
            DropItemInField();
        }

        private void DropItemInField()
        {
            if (Model.ItemsInField.Count < kMaxItemsInField)
            {
                if (_lastDropItemIndex >= Controller.BattleField.TotalPointsForItemSpawn)
                {
                    _lastDropItemIndex = 0;
                }
                var dropPos = Controller.BattleField.GetPointForItem(_lastDropItemIndex);
                DropItem(GameItemCategory.Helmet, dropPos);
                _lastDropItemIndex++;
            }
            var timeForDropItem = UnityEngine.Random.Range(kMinDropInterval, kMinDropInterval);
            ScheduledUpdate(timeForDropItem);
        }

        protected override void OnScheduledUpdate()
        {
            base.OnScheduledUpdate();
            DropItemInField();
        }

        protected override void OnGameMsgReceived(GameMessageBase message)
        {
            switch (message.Type)
            {
                case GameMsgType.Died:
                    OnTankDied(message as TankDiedMsg);
                    break;
                case GameMsgType.PickupGameItem:
                    OnPickUpItem(message as PickUpGameItemMsg);
                    break;
            }
        }

        private void OnTankDied(TankDiedMsg message)
        {
            DropItem(GameItemCategory.Skin, Model.UnitsInBattle[message.ClientId].Position);
        }

        private void OnPickUpItem(PickUpGameItemMsg message)
        {
            var item = message.DropInfo;
            Model.RemoveItem(item.WorldId);

            string userName = Model.ConIdToUserName[message.ClientId];
            PlayerInfo playerData = Storage.Get(userName);

            PlayerItemInfo itemInfo = new PlayerItemInfo(item.CatalogId, item.WorldId);

            playerData.Inventory.AddItem(itemInfo);
            Storage.Change(playerData);

            Server.SendToAllExept(message, message.ClientId);
        }

        private void DropItem(GameItemCategory category, Vector3 pos)
        {
            var dropItem = new DropItemInfo
            {
                CatalogId = GameItemTypeExtentions.GetRandomItem(category),
                Pos = pos,
                WorldId = Storage.GetUniqueWorldId()
            };
            Model.AddItem(dropItem);
            var dropItemMessage = new CreateDropItemMsg { DropInfo = dropItem };
            Server.SendToAll(dropItemMessage);
        }
    }
}