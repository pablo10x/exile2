// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UniRx.Examples
{
    public class Sample12ReactiveProperty : MonoBehaviour
    {
        // Open Sample12Scene. Set from canvas
        [FormerlySerializedAs("MyButton")] public Button myButton;
        [FormerlySerializedAs("MyToggle")] public Toggle myToggle;
        [FormerlySerializedAs("MyInput")] public InputField myInput;
        [FormerlySerializedAs("MyText")] public Text myText;
        [FormerlySerializedAs("MySlider")] public Slider mySlider;

        // You can monitor/modifie in inspector by SpecializedReactiveProperty
        [FormerlySerializedAs("IntRxProp")] public IntReactiveProperty intRxProp = new IntReactiveProperty();

        [FormerlySerializedAs("StringRxProp")] public StringReactiveProperty stringRxProp = new StringReactiveProperty();
        Enemy _enemy = new Enemy(1000);

        void Start()
        {
            
            stringRxProp.Where(x => x.Length > 5).Subscribe(x => Debug.Log(x)).AddTo(this);
            // UnityEvent as Observable
            // (shortcut, MyButton.OnClickAsObservable())
            myButton.onClick.AsObservable().Subscribe(_ => _enemy.CurrentHp.Value -= 99);

            // Toggle, Input etc as Observable(OnValueChangedAsObservable is helper for provide isOn value on subscribe)
            // SubscribeToInteractable is UniRx.UI Extension Method, same as .interactable = x)
            myToggle.OnValueChangedAsObservable().SubscribeToInteractable(myButton);

            // input shows delay after 1 second
#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
            myInput.OnValueChangedAsObservable()
#else
            MyInput.OnValueChangeAsObservable()
#endif
                .Where(x => x != null)
                .Delay(TimeSpan.FromSeconds(1))
                .SubscribeToText(myText); // SubscribeToText is UniRx.UI Extension Method

            // converting for human visibility
            mySlider.OnValueChangedAsObservable()
                .SubscribeToText(myText, x => Math.Round(x, 2).ToString());

            // from RxProp, CurrentHp changing(Button Click) is observable
            _enemy.CurrentHp.SubscribeToText(myText);
            _enemy.IsDead.Where(isDead => isDead == true)
                .Subscribe(_ =>
                {
                    myToggle.interactable = myButton.interactable = false;
                });

            // initial text:)
            intRxProp.SubscribeToText(myText);
        }
    }

    // Reactive Notification Model
    public class Enemy
    {
        public IReactiveProperty<long> CurrentHp { get; private set; }

        public IReadOnlyReactiveProperty<bool> IsDead { get; private set; }

        public Enemy(int initialHp)
        {
            // Declarative Property
            CurrentHp = new ReactiveProperty<long>(initialHp);
            IsDead = CurrentHp.Select(x => x <= 0).ToReactiveProperty();
        }
    }
}

#endif