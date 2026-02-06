using UnityEngine;

public class Client : MonoBehaviour
{
    public int money;
    public int happiness;
    public int patience;
    public int thirst;
    public int hunger;
    public int intoxication;
    public int health;
    public int age;
    public int gender;
    public int nationality;
    public enum State { Waiting, Eating, Drinking, Happy, Angry, Sad, Hungry, Thirsty, Tired, Bored, Drunk, Sick, Dead, Leaving, Paying }
    public State state;
    public GameObject table;
    public GameObject chair;
    public GameObject waiter;
    void Start()
    {
        
    }

    void Update()
    {
        
    }
    public void WaitOnTable()
    {
        
    }
}
