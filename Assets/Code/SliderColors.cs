using UnityEngine;
using UnityEngine.UI;

//TODO: Assign the slider value from the Typing.cs script (for some added fun, transition the value over time)
//TODO: Fade out while typing

//TODO: Idea: Add a colored underline to give the user a useful representation of what the current key practice confidence is compared to the rest (or what each of the values represent compared to the average)

public class SliderColors : MonoBehaviour {
    Slider slider;
    Image image;

    void Start() {
        slider = GetComponent<Slider>();
        image = slider.fillRect.GetComponent<Image>();
    }
    
    public void UpdateColorGreenToRed(){
        if (!slider) {
            Start();
        }
        image.color = slider.value switch{
            < .5f => Color.Lerp(
                Color.green,
                Color.yellow,
                Mathf.Sqrt(slider.value * 2)
            ),
            _ => Color.Lerp(
                Color.yellow,
                Color.red, 
                Mathf.Pow((slider.value - .5f) * 2, 2)
            )
        };
    }
    public void UpdateColorRedToGreen(){
        if (!slider) {
            Start();
        }
        image.color = slider.value switch{
            < .5f => Color.Lerp(
                Color.red,
                Color.yellow,
                Mathf.Sqrt(slider.value * 2)
            ),
            _ => Color.Lerp(
                Color.yellow,
                Color.green,
                Mathf.Pow((slider.value - .5f) * 2, 2)
            )
        };
    }
}
