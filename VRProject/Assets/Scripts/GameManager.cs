using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Messaging;

public class GameManager : MonoBehaviour
{
    // Helper Signs
    public List<GameObject> helperSigns;
    private static List<GameObject> helperSignsStatic;

    // Sound effects
    public List<AudioClip> clickSound;
    private static List<AudioClip> clickSoundsStatic;
    public AudioClip gameOverSound;
    private static AudioClip gameOverSoundStatic;

    // Materials
    public List<Material> blockColours;
    public List<Material> bluePrintColours;
    public static List<Material> blockColoursStatic;

    // A list of all blocks in the game world
    public List<Block> allBlocks;
    public static List<Block> allBlocksStatic;

    // A list of all blue print blocks in the game world
    public List<MeshRenderer> bluePrintBlocks;
    private static List<MeshRenderer> bluePrintBlocksStatic;

    // A list of blueprints
    public List<BluePrint> bluePrints;
    private static List<BluePrint> bluePrintsStatic;

    // A list of blueprints
    public List<HitBox> hitboxes;
    private static List<HitBox> hitboxesStatic;

    // Game state
    public static int numOfPlayers = 4;
    public static int score = 0;
    // Current level
    public static int currLevel;

    // Event that randomly changes roles of the players 
    UnityEvent role_change;
    RoleManager role_manager;

    // Queue containing objects to be spawned on the conveyer belt
    public static Queue<Block> conveyerQueue = new Queue<Block>();


    // Networking components
    public NetworkId NetworkId => new NetworkId("a13ba05dbb9ef8fc");
    private NetworkContext context;

    // Blueprint Room Door
    public BPRoomDoor bPRoomDoor;
    public static BPRoomDoor bPRoomDoorStatic;

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);

        // Initalize static variables
        helperSignsStatic = helperSigns;
        clickSoundsStatic = clickSound;
        blockColoursStatic = blockColours;
        allBlocksStatic = allBlocks;
        gameOverSoundStatic = gameOverSound;
        bluePrintsStatic = bluePrints;
        bluePrintBlocksStatic = bluePrintBlocks;
        hitboxesStatic = hitboxes;
        bPRoomDoorStatic = bPRoomDoor;

        // Set up role components
        role_manager = GameObject.Find("RoleManager").GetComponent<RoleManager>();
        role_change = new UnityEvent();
        role_change.AddListener(OnRoleChange);
    }

    void OnRoleChange()
    {
        role_manager.ShuffleRoles();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Message sent to everyone to start the game
    struct Message
    {
        public bool start;

        public Message(bool start)
        {
            this.start = start;
        }
    }

    public void StartGame()
    {
        Debug.Log("Starting Game!");
        // Remove any unnecessary signs in the environment
        foreach (GameObject sign in helperSignsStatic) // Loop through List with foreach
        {
            Destroy(sign);
        }

        // Colour all blocks in scene
        List<int> colours = role_manager.GetAvatarColourIndexes();
        numOfPlayers = colours.Count();
        for (int i = 0; i < allBlocksStatic.Count; i++)
        {
            // How many colours will be used
            int numberOfColours = numOfPlayers;
            if (numOfPlayers > blockColoursStatic.Count)
                numberOfColours = blockColoursStatic.Count;

            // Tell blocks what materials they should use
            int avatarIdx = i % numberOfColours;
            int colourIdx = colours[avatarIdx];
            allBlocksStatic[i].SetColour(colourIdx);


            // Tell blue print blocks what materials they should use
            bluePrintBlocks[i].material = bluePrintColours[colourIdx];

            // Set any child blocks to be the same colour
            foreach (Transform child in bluePrintBlocks[i].gameObject.transform)
            {
                MeshRenderer mesh_rend = child.GetComponent<MeshRenderer>();
                mesh_rend.material = bluePrintColours[colourIdx];
            }

            // Set hitbox correct colours
            if (hitboxes[i] != null)
                hitboxes[i].correctColourIdx = colourIdx;

        }
        // Set up level 1
        currLevel = 0;
        bPRoomDoorStatic.Init();
        NextLevel();

        // Start the conveyer belt
        StartCoroutine(SpawnBlocks());
    }

    // Unity allows to use Start as IEnumerator instead of a void
    private IEnumerator SpawnBlocks()
    {
        // Loops inifitly
        while (true)
        {
            // If there are objects in the conveyer belt queue, spawn it at the start of the conveyer belt
            if (conveyerQueue.Count > 0)
            {
                Block nextBlock = conveyerQueue.Dequeue();
                nextBlock.gameObject.transform.position = new Vector3(-7, 1.35f, 8.75f);
            }

            // Pause for 2 seconds
            yield return new WaitForSeconds(2);
        }

    }



    public static void PlayClickFromPoint(Vector3 position)
    {
        // Get random audio clip
        var random = new System.Random();
        int index = random.Next(clickSoundsStatic.Count - 1);

        // Play clicking sound
        AudioSource.PlayClipAtPoint(clickSoundsStatic[index], position);
    }

    public static void NextLevel()
    {
        // Increase level number
        currLevel++;

        // Reset the clock to 3 minutes
        Clock.ResetClock();

        // Change which blueprint are visible
        foreach (BluePrint bp in bluePrintsStatic)
        {
            bp.UpdateVisibility();
        }

        // Change who is allowed inside the blue print room
        bPRoomDoorStatic.ChangeColour(currLevel);

        // Call the approapriate level method
        if (currLevel == 1)
            Level1();
        else if (currLevel == 2)
            Level2();
        else if (currLevel == 3)
            Level3();
        else
            EndGame();
    }

    private static void Level1()
    {
        // Clear conveyer belt queue
        conveyerQueue.Clear();

        // Add level 1 blocks to the conveyer belt queue
        for (int i = 0; i < 3; i++)
            conveyerQueue.Enqueue(allBlocksStatic[i]);
    }

    private static void Level2()
    {
        // Clear conveyer belt queue
        conveyerQueue.Clear();

        // Move all level 1 blocks away from the play area
        for (int i = 0; i < 3; i++)
        {
            Block nextBlock = allBlocksStatic[i].GetComponent<Block>();
            nextBlock.Release();
            nextBlock.gameObject.transform.position = new Vector3(0, -9.5f, 8.75f);
        }

        // Add level 2 blocks to the conveyer belt queue
        for (int i = 3; i < 15; i++)
            conveyerQueue.Enqueue(allBlocksStatic[i]);
    }

    private static void Level3()
    {
        // Clear conveyer belt queue
        conveyerQueue.Clear();

        // Move all level 2 blocks away from the play area
        for (int i = 3; i < 15; i++)
        {
            Block nextBlock = allBlocksStatic[i].GetComponent<Block>();
            nextBlock.Release();
            nextBlock.gameObject.transform.position = new Vector3(0, -9.5f, 8.75f);
        }

        // Add level 3 blocks to the conveyer belt queue
        for (int i = 15; i < 30; i++)
            conveyerQueue.Enqueue(allBlocksStatic[i]);
    }

    private static void EndGame()
    {
        // Move all level 3 blocks away from the play area
        for (int i = 15; i < 30; i++)
        {
            Block nextBlock = allBlocksStatic[i].GetComponent<Block>();
            nextBlock.Release();
            nextBlock.gameObject.transform.position = new Vector3(0, -9.5f, 8.75f);
        }

        // Player a sounds to indicate the game is complete
        AudioSource.PlayClipAtPoint(gameOverSoundStatic, new Vector3(0,0,0));

        // Change text on score board and clock
        Clock.EndGame();
        SubmissionArea.EndGame();
    }

    // Send start game message
    public void SendMessageUpdate()
    {
        Message startMessage = new Message(true);
        startMessage.start = true;
        context.SendJson(startMessage);
    }

    // Receive message that start the game
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        if (msg.start == true)
            StartGame();
    }
}
