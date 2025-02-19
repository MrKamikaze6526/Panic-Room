using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class InteractableScript : MonoBehaviour
{
    public float radius;
    private bool canInteract;
    public PlayerInput playerInput;
    private InputAction interactButton;
    public CapsuleCollider Collider;
    public GameObject interactScreen;

    void Awake()
    {
        // Move initialization to Awake
        InitControls();
    }

    void Start()
    {
        // Find and validate references
        if (playerInput == null)
        {
            var controlsObj = GameObject.FindGameObjectWithTag("Game Controls");
            if (controlsObj != null)
            {
                playerInput = controlsObj.GetComponent<PlayerInput>();
            }
            else
            {
                Debug.LogError("Game Controls object not found!");
            }
        }

        if (interactScreen == null)
        {
            var screen = GameObject.FindGameObjectWithTag("InteractScreen");
            if (screen != null)
            {
                interactScreen = screen;
            }
            else
            {
                Debug.LogError("Interact Screen not found!");
            }
        }

        canInteract = false;
       
        // Ensure the screen starts hidden
        if (interactScreen != null)
        {
            interactScreen.SetActive(false);
        }
    }

    void InitControls()
    {
        if (playerInput != null)
        {
            interactButton = playerInput.actions["Interact"];
        }
    }

    void OnEnable()
    {
        if (interactButton != null)
        {
            interactButton.Enable();
        }
    }

    void OnDisable()
    {
        if (interactButton != null)
        {
            interactButton.Disable();
        }
    }

    void Update()
    {
        if (interactButton != null && interactButton.WasPressedThisFrame() && canInteract)
        {
            //Interact
            Debug.Log("Interact");
        }

        if (Collider != null)
        {
            Collider.radius = radius;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        canInteract = true;
        if (interactScreen != null)
        {
            interactScreen.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        canInteract = false;
        if (interactScreen != null)
        {
            interactScreen.SetActive(false);
        }
    }
}