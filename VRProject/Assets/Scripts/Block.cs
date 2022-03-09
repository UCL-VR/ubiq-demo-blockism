using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.XR;
using System;
using Ubiq.Rooms;
using System.Linq;

public class Block : MonoBehaviour, IGraspable, INetworkComponent, INetworkObject
{

    private Hand grasped;

    private NetworkContext context;

    // 16-digit hex
    NetworkId INetworkObject.Id => new NetworkId("f8cdefa3a15f5e6d");

    public NetworkId shared_id; 

    private RoomClient client;

    private string last_owner_id; 

    private bool being_grasped = false; 

    struct Message
    {
        public NetworkId who; 
        public Vector3 position;
        public Quaternion rotation;
        public bool being_grasped;
        public string last_owner_id;

        public Message(Vector3 pos, Quaternion rot, NetworkId who, bool bg, string lo_id)
        {
            this.who = who;
            this.position = pos;
            this.rotation = rot;
            this.being_grasped = bg;
            this.last_owner_id = lo_id;
        }
    }

    void INetworkComponent.ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();

        // update position only if
        // the message comes from an object of the same shared ID
        if (msg.who == shared_id)
        {
            transform.localPosition = msg.position;
            transform.rotation = msg.rotation;
            being_grasped = msg.being_grasped;
            last_owner_id = msg.last_owner_id;
        } 
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);

        client = context.scene.GetComponentInChildren<RoomClient>();

        client.OnPeerAdded.AddListener(OnPeerAdded);

        shared_id = new NetworkId((uint)(Math.Pow(10, 3)*(transform.localPosition.x + 
                                        transform.localPosition.y + 
                                        transform.localPosition.z) ));
    }

    private void SendMessageUpdate()
    {
        Message message;
        message.position = transform.localPosition;
        message.rotation = transform.rotation;
        message.who = shared_id;
        message.being_grasped = being_grasped;
        message.last_owner_id = last_owner_id;

        context.SendJson(message);
    }

    private void OnPeerAdded(IPeer peer)
    {

        if (client.Me.UUID == last_owner_id)
        {
            SendMessageUpdate();
        }
    }

    void IGraspable.Grasp(Hand controller)
    {
        if (being_grasped)
        {
            return; 
        }

        var avatar_manager = GameObject.Find("Avatar Manager").GetComponent<Ubiq.Avatars.AvatarManager>();
        var avatars = avatar_manager.Avatars;
        Ubiq.Avatars.Avatar local_avatar = null; 

        foreach (var avatar in avatars)
        {
            if (avatar.IsLocal)
            {
                local_avatar = avatar;
            }
        }

        if (!local_avatar.GetComponentInChildren<BuilderAvatar>().can_avatar_grasp)
        {
            return; 
        } 

        grasped = controller;
        being_grasped = true;

        last_owner_id = client.Me.UUID;

        SendMessageUpdate();
    }

    void IGraspable.Release(Hand controller)
    {
        grasped = null;
        being_grasped = false;

        SendMessageUpdate();
    }

    // Update is called once per frame
    void Update()
    {
        // If the block is held by a player
        if (being_grasped && grasped)
        {
            // Match the position and orientation of the hand
            transform.localPosition = grasped.transform.position;
            transform.rotation = grasped.transform.rotation;

            // Networking code
            SendMessageUpdate();
        }
    }
}
