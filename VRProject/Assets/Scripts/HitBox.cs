using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.XR;
using Ubiq.Rooms;

public class HitBox : MonoBehaviour
{

    private GameObject thisHitBox;
    public List<GameObject> pieces;
    Collider thisCollider;
    List<Collider> pieceColliders = new List<Collider>();
    public bool filled = false;
    public int correctColourIdx;

    // Networking 
    public NetworkId shared_id;
    private NetworkContext context;
    // 16-digit hex
    public NetworkId NetworkId => new NetworkId("f8cdba13a15f5e6d");

    struct Message
    {
        public NetworkId who;
        public NetworkId blockId;

        public Message(NetworkId who, NetworkId blockId)
        {
            this.who = who;
            this.blockId = blockId;
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        if (msg.who == shared_id)
        {
            NetworkId blockId = msg.blockId;
            foreach (Block b in GameManager.allBlocksStatic)
            {
                if (b.shared_id == blockId)
                {
                    FillHitBox(b.gameObject);
                }
            }
        }
    }

    public void SendMessageUpdate(Block block)
    {
        Message message;
        message.who = shared_id;
        message.blockId = block.shared_id;
        context.SendJson(message);
    }

    // Start is called before the first frame update
    void Start()
    {
        //Check that the peice GameObject exists and fetch the Collider
        foreach (GameObject obj in pieces)
        {
            pieceColliders.Add(obj.GetComponent<Collider>());
        }

        //Check that the hitbox GameObject exists in the Inspector and fetch the Collider
        thisHitBox = gameObject;
        if (thisHitBox != null)
            thisCollider = thisHitBox.GetComponent<Collider>();

        // Networking
        context = NetworkScene.Register(this);
        shared_id = new NetworkId((uint)(Math.Pow(10, 3) * (transform.position.x +
                                transform.position.y +
                                Math.Pow(10, 2) * transform.position.z)));

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        for (int i = 0; i < pieceColliders.Count; i++)
        {
            Collider collider = pieceColliders[i];
            Block block = pieces[i].GetComponent<Block>();
            Block parentBlock = transform.parent.GetComponent<Block>();
            if (GetIntersectionPercent(collider, thisCollider) > 0.5 && !filled && !block.filling && (parentBlock.grasped != null || block.grasped != null))
            {
                SendMessageUpdate(block);
                FillHitBox(pieces[i]);
                if (transform.childCount > 0)
                    filled = true;
            }
        }
    }

    public void FillHitBox(GameObject piece)
    {
        // Remove grasp for both players
        Block b = piece.GetComponent<Block>();
        Block parentBlock = transform.parent.GetComponent<Block>();
        b.Release();
        b.rootBlock = parentBlock;
        parentBlock.Release();
        b.filling = true;

        // Set parent of connection peice to this hitbox
        piece.transform.position = thisHitBox.transform.position;
        piece.transform.rotation = thisHitBox.transform.rotation;
        piece.transform.SetParent(thisHitBox.transform);

        //Remove rigid body from connecting peice
        var rb = piece.GetComponent<Rigidbody>();
        Destroy(rb);

        //Remove block script from connecting piece
        //var blk_scr = piece.GetComponent<Block>();
        //Destroy(blk_scr);

        // Play sound effect
        GameManager.PlayClickFromPoint(transform.position);

        // Update position for both blocks to ensure consistancy for all players
        b.SendMessageUpdate();
        parentBlock.SendMessageUpdate();

        // Update colour string
        parentBlock.color += "," + b.color;
        b.color = "";
    }

    // Estimates percentage of two colliders that are intersecting (may need to be improved)
    private float GetIntersectionPercent(Collider c1, Collider c2)
    {
        if (c1.bounds.Intersects(c2.bounds))
        {
            // Get minimums and maximums of the bounding boxes
            Vector3 min1 = c1.bounds.min;
            Vector3 max1 = c1.bounds.max;
            Vector3 min2 = c2.bounds.min;
            Vector3 max2 = c2.bounds.max;

            // Calculate intersection in each axis
            float x_intersection = Math.Max(max1.x - min2.x, max2.x - min1.x);
            float y_intersection = Math.Max(max1.y - min2.y, max2.y - min1.y);
            float z_intersection = Math.Max(max1.z - min2.z, max2.z - min1.z);

            // Calculate volumn of each bounding box
            float volumn1 = (max1.x - min1.x) * (max1.y - min1.y) * (max1.z - min1.z);
            float volumn2 = (max2.x - min2.x) * (max2.y - min2.y) * (max2.z - min2.z);

            // Take the smallest bouding box (a bounding box is aligned with world coordinates so is bigger than the block)
            float smallestVolumn = Math.Min(volumn1, volumn2);

            // Return an estimate of the percent of two colliders are intersecting each other
            return smallestVolumn / (x_intersection * y_intersection * z_intersection);
        }

        return 0;
    }
}
