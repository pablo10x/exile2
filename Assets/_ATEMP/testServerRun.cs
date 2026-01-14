using System;
using UnityEngine;

public class testServerRun : MonoBehaviour {
    private void Awake() {
        
    }

    private void Start() {

        Console.Clear();
        if (IsLinux()) {
            Console.Clear();
            
            
            Console.WriteLine("Server is running on Linux.");
            Console.BackgroundColor = ConsoleColor.DarkBlue; 
            Console.WriteLine($"Server Version {Application.version}");
    }
        else {

            
            Console.WriteLine($"Application platform: {Application.platform}");
        }
    }

    public bool IsLinux() {
        return Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor;
    }

    public void OnStartServer()
    {
        //base.OnStartServer();
        
        Debug.Log("Server has started successfully.");
    
    }
}