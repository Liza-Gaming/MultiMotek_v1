using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemAssets : MonoBehaviour
{
    public static ItemAssets Instance { get; private set; }
    public AudioClip useItemSound;
    public AudioClip useInsulinSound;
    private void Awake()
    {
        Instance = this;
    }

    public Transform pfItemWorld;
    public Sprite Insulin;
    public Sprite SugarBag;
    public Sprite Apple;
    public Sprite Bread;
    public Sprite EnergyDrink;
    public Sprite ChocolateCup;
    public Sprite CokeCup;
    public Sprite Candy;
    public Sprite LineChocolate;
    public Sprite ChickenLeg;
    public Sprite Fish;
    public Sprite Sausages;
    public Sprite DietYogurt;
    public Sprite Banana;
    public Sprite WaterMelon;
    public Sprite BeanBag;
    public Sprite Bamba;
    public Sprite Baigale;
    public Sprite Icecream;
    public Sprite Cucamber;
    public Sprite Carrot;
    public Sprite Tomato;

}
