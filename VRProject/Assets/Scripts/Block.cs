using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.XR;
using System;
using Ubiq.Rooms;

public class Block : MonoBehaviour, IGraspable
{
    public Hand grasped;

    private NetworkContext context;

    public Block rootBlock = null;

    // 16-digit hex
    NetworkId NetworkId => new NetworkId("f8cdefa3a15f5e6d");

    public NetworkId shared_id;

    private RoomClient client;

    private string last_owner_id;

    public bool being_grasped = false;

    public Rigidbody rb;

    public string color;

    public bool filling;

    public int colourIdx;

    // Block messaged used to communicate position, ownership and physics
    struct Message
    {
        public NetworkId who;
        public Vector3 position;
        public Quaternion rotation;
        public bool being_grasped;
        public string last_owner_id;
        public bool is_kinematic;

        public Message(Vector3 pos, Quaternion rot, NetworkId who, bool bg, string lo_id, bool is_kinematic)
        {
            this.who = who;
            this.position = pos;
            this.rotation = rot;
            this.being_grasped = bg;
            this.last_owner_id = lo_id;
            this.is_kinematic = is_kinematic;
        }
    }

    // Update block transform, ownership and physics
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();

        // update position only if
        // the message comes from an object of the same shared ID
        if (msg.who == shared_id)
        {
            rootBlock.gameObject.transform.position = msg.position;
            rootBlock.gameObject.transform.rotation = msg.rotation;
            being_grasped = msg.being_grasped;
            last_owner_id = msg.last_owner_id;
            rootBlock.rb.isKinematic = msg.is_kinematic;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Set up networking componenets
        context = NetworkScene.Register(this);

        client = context.Scene.GetComponentInChildren<RoomClient>();

        client.OnPeerAdded.AddListener(OnPeerAdded);

        shared_id = new NetworkId((uint)(Math.Pow(10, 3) * (transform.position.x +
                                        transform.position.y +
                                        transform.position.z)));

        // Initialize the rigid body
        rb = GetComponent<Rigidbody>();

        // Root block is always initally itself
        rootBlock = this;
    }

    // Send info about block transform, ownership and physics
    public void SendMessageUpdate()
    {
        Message message;
        message.position = transform.position;
        message.rotation = transform.rotation;
        message.who = shared_id;
        message.being_grasped = being_grasped;
        message.last_owner_id = last_owner_id;
        message.is_kinematic = rootBlock.rb.isKinematic;
        context.SendJson(message);
    }

    // Called when a new player joins the room
    private void OnPeerAdded(IPeer peer)
    {
        if (client.Me.uuid == last_owner_id)
        {
            SendMessageUpdate();
        }
    }

    void IGraspable.Grasp(Hand controller)
    {
        // If the block is already being grasped then it cant be grasped again
        if (being_grasped)
        {
            return;
        }

        // If the block is the wrong colour, it cannot be picked up
        var my_color = client.Me["blockism.color"];
        if (string.IsNullOrEmpty(my_color) || !color.Contains(my_color))
        {
            return;
        }

        // Update grasp information 
        grasped = controller;
        being_grasped = true;

        // Turn off gravity when being held
        rootBlock.rb.isKinematic = true;

        // Update the most recent owner
        last_owner_id = client.Me.uuid;

        // Send message to all other peers
        SendMessageUpdate();

    }

    void IGraspable.Release(Hand controller)
    {
        Release();
    }

    public void Release()
    {
        // Stops objects from falling out of the world
        if (grasped)
        {
            bool outOfRange = HandOutOfRange(grasped);

            if (outOfRange)
            {
                rootBlock.transform.position = new Vector3(0, 1, 0);
                rootBlock.transform.rotation = new Quaternion(0, 0, 0, 0);
            }
        } 

        // Remove grasp info
        grasped = null;
        being_grasped = false;

        // Turn gravity back on
        this.rootBlock.rb.isKinematic = false;

        // Send block info to other peers
        SendMessageUpdate();
    }

    private bool HandOutOfRange(Hand grasped)
    {
        return grasped.transform.position[0] > 10
            || grasped.transform.position[0] < -9
            || grasped.transform.position[1] < 0.3
            || grasped.transform.position[2] > 9.5
            || grasped.transform.position[2] < -18;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // If the block is held by a player
        if (being_grasped && grasped && last_owner_id == client.Me.uuid)
        {
            // Match the position and orientation of the hand
            rootBlock.transform.position = grasped.transform.position;
            rootBlock.transform.rotation = grasped.transform.rotation;

            // Networking code
            SendMessageUpdate();
        }

        // Brute force approach to ensure that connecting blocks stay in the correct position
        if (filling)
        {
            transform.localPosition = new Vector3(0, 0, 0);
            transform.localEulerAngles= new Vector3(0, 0, 0);
            transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public void SetColour(int colour)
    {
        // Set this blocks colour
        MeshRenderer mesh_rend = GetComponent<MeshRenderer>();
        mesh_rend.material = GameManager.blockColoursStatic[colour];
        colourIdx = colour;
        color = GameManager.blockColoursStatic[colour].name.ToLower();

        // Set any child blocks to be the same colour
        foreach (Transform child in transform)
        {
            mesh_rend = child.GetComponent<MeshRenderer>();
            mesh_rend.material = GameManager.blockColoursStatic[colour];
        }
    }
}
