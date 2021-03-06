﻿using System.Collections.Generic;
using Game.BattleField;
using Game.Bullet;
using Game.Camera;
using Game.Decorators;
using Game.Explosions;
using Game.PickupItems;
using Game.Tank;
using Game.Units.Components;
using Game.Units.Components.Standalone;
using PlayerData;
using SHLibrary;
using TankGame.GameClient.UI;
using UnityEngine;

namespace Game
{
    public class GameContext : UnityBehaviourBase
    {
        [SerializeField]
        private TankController _tankPrefab;
        [SerializeField]
        private TankBulletManager _bulletManager;
        [SerializeField]
        private BattleFieldController _battleField;
        [SerializeField]
        private FollowCamera _followCamera;
        [SerializeField]
        private ExplosionEffectsManager _explosionManager;
        [SerializeField]
        private TankHealthBarView _tankHealthBar;
        [SerializeField]
        private GameDecorationsCatalog _decorationCatalog;
        [SerializeField]
        private UnitHelmetCatalog _unitHelmetCatalog;

        public readonly GameUnitCatalog UnitCatalog = new GameUnitCatalog();

        public IBulletManager BulletManager { get { return _bulletManager; } }

        public IBattleField BattleField { get { return _battleField; } }

        public IFollowCamera FollowCamera { get { return _followCamera; } }

        [SerializeField]
        public PickUpItemsManager PickUpManager;

        public IExplosionEffectsManager ExplosionManager
            { get { return _explosionManager; } }

        public void Run()
        {
            BulletManager.BulletStarted += OnBulletStarted;
            BulletManager.Hitted += OnBulletHidded;
            PickUpManager.UnitPicked += OnUnitPicked;
        }

        public void Shutdown()
        {
            BulletManager.BulletStarted -= OnBulletStarted;
            BulletManager.Hitted -= OnBulletHidded;
            PickUpManager.UnitPicked -= OnUnitPicked;
        }

        public ITank CreateTank(bool forPlayer, GameMode mode, List<GameItemType> playerItems)
        {
            ITank newTank = CreateTank(forPlayer, mode);
            TankView tankView = (newTank as TankController).View;

            IUnitSkinDecorator skinDecorator = new TankSkinDecorator(_decorationCatalog, tankView);
            IUnitSkinDecorator helmetDecorator = new TankHelmetDecorator(_unitHelmetCatalog, tankView);

            foreach (var itemType in playerItems)
            {
                skinDecorator.ApplySkinItem(itemType);
                helmetDecorator.ApplySkinItem(itemType);
            }

            return newTank;
        }

        public ITank CreateTank(bool forPlayer, GameMode mode)
        {
            var tank = GameObject.Instantiate<TankController>(_tankPrefab);
            var healt = tank.gameObject.GetComponent<TankHealtComponent>();
            var unitType = forPlayer ? GameUnitType.Player : GameUnitType.Oponent;
            UnitCatalog.AddAs(healt, unitType);
            var input = GetTankInput(tank, forPlayer, mode);
            var weapon = tank.gameObject.GetComponent<TankWeaponComponent>();

            BulletManager.AddWeapon(weapon);
            tank.Died += OnTankDied;
            tank.Healt = healt;
            tank.Unit = healt;
            tank.Input = input;
            tank.Weapon = weapon;
            tank.gameObject.name = string.Format("{0} tank", unitType);
            tank.transform.SetParent(transform, true);
            
            if (forPlayer)
            {
                _tankHealthBar.ApplyModel(tank.Model);
            }

            return tank as ITank;
        }

        public void DestroyTank(ITank tank)
        {
            tank.Died -= OnTankDied;
            var tankObject = (tank as TankController).gameObject;
            BulletManager.RemoveWeapon(tankObject.GetComponent<TankWeaponComponent>());
            tank.Broke();
            GameObject.Destroy(tankObject);
        }

        private void OnTankDied(ITank tank)
        {
            ExplosionManager.Play(tank.Pos, ExplosionEffectType.TankExplosion); 
        }

        private IUnitInsideInputComponent GetTankInput(TankController tank, bool forPlayer,
            GameMode mode)
        {
            if (forPlayer)
            {
                #if UNITY_ANDROID || UNITY_IOS || UNITY_IPHONE
                    return tank.gameObject.AddComponent<PlayerMobileInput>();
                #else
                    return tank.gameObject.AddComponent<PlayerStandaloneInput>();
                #endif
            }
            else if (mode == GameMode.Offline)
            {
                return tank.gameObject.AddComponent<BotTankInput>();
            }
            else
            {
                return tank.gameObject.AddComponent<EmptyTankInput>();
            }
        }

        private void OnBulletHidded(Collider target, IBullet bullet)
        {
            ExplosionManager.Play(bullet.Pos, ExplosionEffectType.BulletExplosion);
        }

        private void OnBulletStarted(IBullet bullet)
        {
            ExplosionManager.Play(bullet.Pos, ExplosionEffectType.GunShoot);
        }

        private void OnUnitPicked(int unitId, PickupItem item)
        {
            ExplosionManager.Play(item.transform.position, ExplosionEffectType.ItemPickUp);
        }
    }
}
