using NUnit.Framework.Constraints;
using UnityEngine;

[System.Serializable]
public class Weapongrp : Abstractgrp
{
    [SerializeField] private WeaponType _weaponType;
    [SerializeField] private ItemSlot _bodyPart;
    [SerializeField] private byte _soulshot;
    [SerializeField] private byte _spiritshot;
    [SerializeField] private int _mpConsume;
    [SerializeField] private string _model;
    [SerializeField] private string _texture;
    [SerializeField] private string[] _itemSounds;
    [SerializeField] private int _patk;
    [SerializeField] private int _patkspeed;
    [SerializeField] private int _matk;
    [SerializeField] private int _critRate;
    [SerializeField] private int _isMagicWeapon;
    private int _shield_pdef;
    private int _shield_rate;
    private int _dex;
    public int IsMagicWeapon { get { return _isMagicWeapon; } set { _isMagicWeapon = value; } }

    public ItemSlot BodyPart { get { return _bodyPart; } set { _bodyPart = value; } }
    public WeaponType WeaponType { get { return _weaponType; } set { _weaponType = value; } }
    public byte Soulshot { get { return _soulshot; } set { _soulshot = value; } }
    public byte Spiritshot { get { return _spiritshot; } set { _spiritshot = value; } }
    public int MpConsume { get { return _mpConsume; } set { _mpConsume = value; } }
    public string Model { get { return _model; } set { _model = value; } }
    public string Texture { get { return _texture; } set { _texture = value; } }

    public int PAtk { get { return _patk; } set { _patk = value; } }
    public int Matk { get { return _matk; } set { _matk = value; } }
    public int ShieldPdef { get { return _shield_pdef; } set { _shield_pdef = value; } }
    public int ShieldRate { get { return _shield_rate; } set { _shield_rate = value; } }
    public int Dex { get { return _dex; } set { _dex = value; } }

    public int PAtkSpeed { get { return _patkspeed; } set { _patkspeed = value; } }
    public int CriticalRate { get { return _critRate; } set { _critRate = value; } }
    public string[] ItemSounds { get { return _itemSounds; } set { _itemSounds = value; } }

    public string GetSpeedName()
    {
        if(_patkspeed != 0)
        {
            //default 379
            if(_patkspeed > 325)
            {
                return  "Fast";
            }else if (_patkspeed <= 293)
            {
                return "Slow";
            }else if (_patkspeed >= 293 & _patkspeed <= 325)
            {
                return "Normal";
            }
        }

        return "None";
    }
}
