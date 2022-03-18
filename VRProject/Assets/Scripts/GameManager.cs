﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Sound effects
    public List<AudioClip> clickSound;
    private static List<AudioClip> clickSoundsStatic;

    // Materials
    public List<Material> blockColours;
    public List<Material> bluePrintColours;
    public static List<Material> blockColoursStatic;

    // A list of all blocks in the game world
    public List<Block> allBlocks;
    private static List<Block> allBlocksStatic;

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
    static int numOfPlayers = 4;
    public static Queue<Block> conveyerQueue = new Queue<Block>();

    // Current level
    public static int currLevel;

    // Start is called before the first frame update
    void Start()
    {
        // Initalize static variables
        clickSoundsStatic = clickSound;
        blockColoursStatic = blockColours;
        allBlocksStatic = allBlocks;
        bluePrintsStatic = bluePrints;
        bluePrintBlocksStatic = bluePrintBlocks;
        hitboxesStatic = hitboxes;

        StartGame();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {
        // Colour all blocks in scene
        for (int i = 0; i < allBlocksStatic.Count; i++)
        {
            // How many colours will be used
            int numberOfColours = numOfPlayers;
            if (numOfPlayers > blockColoursStatic.Count)
                numberOfColours = blockColoursStatic.Count;

            // Tell blocks what materials they should use
            int colourIdx = i % numberOfColours;
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
        currLevel = 0;

        NextLevel();
        StartCoroutine(SpawnBlocks());
    }

    // Unity allows to use Start as IEnumerator instead of a void
    private IEnumerator SpawnBlocks()
    {
        while (true)
        {
            if (conveyerQueue.Count > 0)
            {
                Block nextBlock = conveyerQueue.Dequeue();
                nextBlock.gameObject.transform.position = new Vector3(-7, 1.35f, 8.75f);
            }
            yield return new WaitForSeconds(2);
        }
        
    }



    public static void PlayClickFromPoint(Vector3 position)
    {
        // Get random audio clip
        var random = new System.Random();
        int index = random.Next(clickSoundsStatic.Count-1);

        // Play clicking sound
        AudioSource.PlayClipAtPoint(clickSoundsStatic[index], position);
    }

    public static void NextLevel()
    {
        currLevel++;
        Clock.ResetClock();
        foreach (BluePrint bp in bluePrintsStatic)
        {
            bp.UpdateVisibility();
        }

        if (currLevel == 1)
            Level1();
        else if (currLevel == 2)
            Level2();
        else if (currLevel == 3)
            Level3();
    }

    private static void Level1()
    {
        for (int i = 0; i < 3; i++)
            conveyerQueue.Enqueue(allBlocksStatic[i]);
    }

    private static void Level2()
    {

        conveyerQueue.Clear();

        for (int i = 0; i < 3; i++)
        {
            Block nextBlock = allBlocksStatic[i].GetComponent<Block>();
            nextBlock.gameObject.transform.position = new Vector3(0, -99.5f, 8.75f);
        }

        for (int i = 3; i < 15; i++)
            conveyerQueue.Enqueue(allBlocksStatic[i]);
    }

    private static void Level3()
    {
        conveyerQueue.Clear();

        for (int i = 3; i < 15; i++)
        {
            Block nextBlock = allBlocksStatic[i].GetComponent<Block>();
            nextBlock.gameObject.transform.position = new Vector3(0, -99.5f, 8.75f);
        }

        for (int i = 15; i < 30; i++)
            conveyerQueue.Enqueue(allBlocksStatic[i]);
    }
}
