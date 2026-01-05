using System;
using Microsoft.Xna.Framework;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace SpaceTanks;

public class ComputerTankAgent
{
    private readonly Tank _playerTank;
    private readonly Tank _computerTank;
    private readonly TankPhysics _computerPhysics;
    private readonly Func<bool> _fireMissile;
    private readonly Func<bool> _fireHomingMissile;
    private readonly Func<bool> _fireGrenade;
    private readonly Random _random = new Random();

    private const float PreferredDistance = 350f;
    private const float DistanceTolerance = 60f;
    private const float MovementForce = 6f;
    private const float AimTolerance = 0.04f;
    private const float CloseRangeThreshold = 250f;

    public ComputerTankAgent(
        Tank playerTank,
        Tank computerTank,
        TankPhysics computerPhysics,
        Func<bool> fireMissile,
        Func<bool> fireHomingMissile,
        Func<bool> fireGrenade
    )
    {
        _playerTank = playerTank ?? throw new ArgumentNullException(nameof(playerTank));
        _computerTank = computerTank ?? throw new ArgumentNullException(nameof(computerTank));
        _computerPhysics =
            computerPhysics ?? throw new ArgumentNullException(nameof(computerPhysics));
        _fireMissile = fireMissile ?? (() => false);
        _fireHomingMissile = fireHomingMissile ?? (() => false);
        _fireGrenade = fireGrenade ?? (() => false);
    }

    public void Update(GameTime gameTime)
    {
        if (_computerTank.Destroyed)
            return;

        UpdateMovement();
        UpdateAiming();
        UpdateShooting();
    }

    private void UpdateMovement()
    {
        if (_playerTank == null || _playerTank.Destroyed)
            return;

        float deltaX = _playerTank.Position.X - _computerTank.Position.X;
        float distance = Math.Abs(deltaX);

        if (distance > PreferredDistance + DistanceTolerance)
        {
            float direction = Math.Sign(deltaX);
            _computerPhysics.ApplyForce(new AetherVector2(direction * MovementForce, 0f));
        }
        else if (distance < PreferredDistance - DistanceTolerance)
        {
            float direction = -Math.Sign(deltaX);
            _computerPhysics.ApplyForce(new AetherVector2(direction * MovementForce, 0f));
        }
        else
        {
            _computerPhysics.DampenMovement();
        }
    }

    private void UpdateAiming()
    {
        if (_playerTank == null || _playerTank.Destroyed)
        {
            _computerPhysics.StopRotatingTurret();
            return;
        }

        Vector2 turretPos = _computerTank.Gun.Position;
        Vector2 targetPos = _playerTank.Position;
        Vector2 delta = targetPos - turretPos;

        if (delta.LengthSquared() < float.Epsilon)
            return;

        float desiredAngle = (float)Math.Atan2(delta.Y, delta.X);
        float turretAngle = _computerPhysics.GetTurretRotation();
        float angleDelta = MathHelper.WrapAngle(desiredAngle - turretAngle);

        if (Math.Abs(angleDelta) > AimTolerance)
        {
            float direction = Math.Sign(angleDelta);
            _computerPhysics.RotateTurret(direction);
        }
        else
        {
            _computerPhysics.StopRotatingTurret();
        }
    }

    private void UpdateShooting()
    {
        if (_playerTank == null || _playerTank.Destroyed)
            return;

        TryFireSelectedWeapon();
    }

    private bool TryFireSelectedWeapon()
    {
        WeaponType weapon = ChooseWeapon();
        if (FireWeapon(weapon))
            return true;

        if (weapon != WeaponType.Missile)
            return FireWeapon(WeaponType.Missile);

        return false;
    }

    private WeaponType ChooseWeapon()
    {
        if (_playerTank == null)
            return WeaponType.Missile;

        float distance = Vector2.Distance(_playerTank.Position, _computerTank.Position);

        double roll = _random.NextDouble();

        if (distance <= CloseRangeThreshold)
        {
            return roll < 0.6 ? WeaponType.Homing : WeaponType.Missile;
        }

        if (roll < 0.2)
            return WeaponType.Grenade;
        if (roll < 0.4)
            return WeaponType.Homing;
        return WeaponType.Missile;
    }

    private bool FireWeapon(WeaponType weapon)
    {
        return weapon switch
        {
            WeaponType.Missile => _fireMissile(),
            WeaponType.Homing => _fireHomingMissile(),
            WeaponType.Grenade => _fireGrenade(),
            _ => false,
        };
    }

    private enum WeaponType
    {
        Missile,
        Homing,
        Grenade,
    }
}
