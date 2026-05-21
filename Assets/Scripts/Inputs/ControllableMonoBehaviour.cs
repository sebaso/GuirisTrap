using UnityEngine;

public class ControllableMonoBehaviour : MonoBehaviour
{
    public virtual void OnMove(Vector2 direction) { }
    public virtual void OnLook(Vector2 direction) { }
    public virtual void OnInteractDown()          { }
    public virtual void OnInteractUp()            { }
    public virtual void OnCancelDown()            { }  // Q 
    public virtual void OnSubmitDown()            { }  // Enter 
    public virtual void OnJumpDown()              { }
    public virtual void OnJumpUp()                { }
    public virtual void OnAttackDown()            { }  
    public virtual void OnAttackUp()              { }  
    public virtual void OnCrouchDown()            { }  
    public virtual void OnCrouchUp()              { }  
}