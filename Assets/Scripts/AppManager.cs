using UnityEngine;

/// <summary>
/// AppManager is responsible for transitioning between different menus, opening and closing the
/// help panel, detecting user input and starting the game.
/// </summary>
public class AppManager : MonoBehaviour
{
	[SerializeField] private LevelModel[] difficultyModels;
	
	[SerializeField] private Game game;
	[SerializeField] private GameObject startMenu;
	[SerializeField] private GameObject sizeMenu;
	[SerializeField] private GameObject defeatPanel;
	[SerializeField] private GameObject victoryPanel;
	[SerializeField] private GameObject instructionsPanel;
	[SerializeField] private GameObject helpIcon;

	public void HandleStartButtonClick()
	{
		startMenu.SetActive(false);
		sizeMenu.SetActive(true);
	}

	public void HandleDifficultySelection(int difficultyIndex)
	{
		sizeMenu.SetActive(false);
		helpIcon.SetActive(true);
		game.Build(difficultyModels[difficultyIndex]);
		game.gameObject.SetActive(true);
		game.Play();
	}

	public void HandleHelpButtonClick()
	{
		game.IsGameRunning = false;
		instructionsPanel.SetActive(true);
	}

	public void HandleInstructionCloseButtonClick()
	{
		game.IsGameRunning = true;
		instructionsPanel.SetActive(false);
	}

	public void HandleReplayButtonClick()
	{
		defeatPanel.SetActive(false);
		victoryPanel.SetActive(false);
		game.Play();
	}

	public void HandleExitButtonClick()
	{
		helpIcon.SetActive(false);
		defeatPanel.SetActive(false);
		victoryPanel.SetActive(false);
		game.gameObject.SetActive(false);
		startMenu.SetActive(true);
	}

	private void HandleVictory()
	{
		//Displays the DisplayVictoryPanel after a delay to allow time for the player to see
		// all the revealed squares.
		Invoke("OpenVictoryPanel", 0.2f);
	}

	private void OpenVictoryPanel()
	{
		victoryPanel.SetActive(true);
	}

	private void OpenDefeatPanel()
	{
		defeatPanel.SetActive(true);
	}
	
	private void Start()
	{
		game.GameWon += HandleVictory;
		game.GameLost += OpenDefeatPanel;
	}

	private void Update ()
	{
		if (game.IsGameRunning)
			DetectMouseInput();
	}
	
	private void DetectMouseInput()
	{
		//detect left-clicks
		if (Input.GetMouseButtonDown(0))
		{
			var hit = CastRay();

			if (hit)
			{
				var cell = hit.collider.gameObject.GetComponent<Cell>();

				game.HandleCellLeftClick(cell);
			}
		}
		//detect right-clicks
		else if (Input.GetMouseButtonDown(1))
		{
			var hit = CastRay();

			if (hit)
			{
				game.HandleCellRightClick(hit.collider.gameObject.GetComponent<Cell>());
			}
		}
	}

	/// <summary>
	/// Casts a ray to detected whether there the mouse is over a cell
	/// </summary>
	private static RaycastHit2D CastRay() 
	{
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		
		return Physics2D.Raycast(ray.origin, ray.direction, 10000);
	}
}
