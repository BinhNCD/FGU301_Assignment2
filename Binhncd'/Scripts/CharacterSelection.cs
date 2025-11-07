using UnityEngine;

public class CharacterSelection : MonoBehaviour
{
    public GameObject[] characters;
    public int num;

    public void changeCharacter(int num)
    {
        for (int i = 0; i < characters.Length; i++)
        {
            characters[i].SetActive(false);
        }

        num += num;

        if (num >= characters.Length)
        {
            num = 0;
        }
        
        if (num < 0)
        {
            num = characters.Length - 1;
        }

        characters[num].SetActive(true);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
