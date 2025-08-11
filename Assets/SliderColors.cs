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
    void Update() {
        switch(slider.value) {
            case <.5f:
                image.color = Color.Lerp(Color.red, Color.yellow, slider.value * 2);
                break;
            default:
                image.color = Color.Lerp(Color.yellow, Color.green, (slider.value - .5f) * 2);
                break;
        }
    }
}
