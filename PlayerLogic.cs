using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Отвечает за логику игрока
public class PlayerLogic : IEntityComponent, IInitializableComponent, IStartable, IListenable, IUpdatable, IFixedUpdatable
{
    
    private float _elapsedTimeSlime;
    private float _elapsedTimeAccelerator;
    private bool _isBreakAccelerator = false;
    private CoroutineExecutor _coroutineExecutor;
    private PlayerSimulation _simulation;
    private PlayerAnimationController _animiationController;
    private PlayerInput _input;
    private IEntitySwitchableComponents _switcher;
    private HashSet<Transform> _accelerators = new HashSet<Transform>();
    private Transform _lastHitFragile;
    private Coroutine _waitSlimeLag;
    private Coroutine _waitAcceleratorLag;
    private WaitForSeconds _slimeLag = new WaitForSeconds(0.2f);

    public void Initialize(IEntityComponentAdapter entityComponentAdapter)
    {        
        _input = entityComponentAdapter.GetEntityComponent<PlayerInput>();
        _simulation = entityComponentAdapter.GetEntityComponent<PlayerSimulation>();
        _animiationController = entityComponentAdapter.GetEntityComponent<PlayerAnimationController>();
    }

    public void InitializeSwitcher(IEntitySwitchableComponents switcher, CoroutineExecutor coroutineExecutor)
    {
        _switcher = switcher;
        _coroutineExecutor = coroutineExecutor;
    }

    public void Start()
    {
        _switcher.DisableUpdateComponent(_input);
        _switcher.DisableUpdateComponent(this);
    }

    public void AddListeners()
    {
        EventManager.AddListener(InputEvent.JUMP_PRESSED, OnPlayerJump);
        EventManager.AddListener(InputEvent.ROLL_PRESSED, OnPlayerRoll);

        EventManager.AddListener(GameEvent.GAME_START, OnGameStart);
        EventManager.AddListener(GameEvent.GAME_RESTART, OnGameRestart);
        EventManager.AddListener(GameEvent.GAME_OVER, OnGameOver);
        EventManager.AddListener(GameEvent.GAME_RESUMED, OnGameResume);
        EventManager.AddListener(GameEvent.GAME_PAUSED, OnGamePause);
        EventManager.AddListener(GameEvent.PLAYER_HIT_OBSTACLE, OnPlayerHitObstacle);
        EventManager.AddListener(GameEvent.PLAYER_DROWNED, OnPlayerDrowned);

        EventManager.AddListener(CanvasEvent.CLICK_PLAY, OnClickPlay);
        EventManager.AddListener(CanvasEvent.CLICK_MENU, OnClickMenu);
    }

    public void RemoveListeners()
    {
        EventManager.RemoveListener(InputEvent.JUMP_PRESSED, OnPlayerJump);
        EventManager.RemoveListener(InputEvent.ROLL_PRESSED, OnPlayerRoll);

        EventManager.RemoveListener(GameEvent.GAME_START, OnGameStart);
        EventManager.RemoveListener(GameEvent.GAME_RESTART, OnGameRestart);
        EventManager.RemoveListener(GameEvent.GAME_OVER, OnGameOver);
        EventManager.RemoveListener(GameEvent.GAME_RESUMED, OnGameResume);
        EventManager.RemoveListener(GameEvent.GAME_PAUSED, OnGamePause);
        EventManager.RemoveListener(GameEvent.PLAYER_HIT_OBSTACLE, OnPlayerHitObstacle);
        EventManager.RemoveListener(GameEvent.PLAYER_DROWNED, OnPlayerDrowned);

        EventManager.RemoveListener(CanvasEvent.CLICK_PLAY, OnClickPlay);
        EventManager.RemoveListener(CanvasEvent.CLICK_MENU, OnClickMenu);
    }

    public void Update()
    {
        _simulation.Move(Time.deltaTime);
    }

    public void FixedUpdate()
    {
        _simulation.UpdatePhysics(Time.fixedDeltaTime);
        _animiationController.UpdatePhysicsParameters();
    }

    public void HitFragile(Transform fragileTransform)
    {
        if (fragileTransform.Equals(_lastHitFragile)) return;

        _lastHitFragile = fragileTransform;

        if (_lastHitFragile.TryGetComponent(out IEntityComponentAdapter entity))
            if (entity.TryGetEntityComponent(out FragileCollisionHandler component))
                component.PlayerBreak();
    }

    private void OnGameStart()
    {
        OnGameResume();
        
        _simulation.RunForward();
        _animiationController.PlayerIsRunning();
    }

    private void OnGameOver()
    {
        _switcher.DisableUpdateComponent(_input);

        _animiationController.PlayerStands();
    }

    private void OnGameRestart()
    {
        OnGameResume();
        
        _simulation.MoveToStartPosition();
        _simulation.RunForward();
        _animiationController.ResetAnimation();
        _animiationController.RestartRunAnimation();
        _animiationController.PlayerIsRunning();

        _isBreakAccelerator = false;
    }

    private void OnGameResume()
    {
        _switcher.EnableUpdateComponent(_input);
        _switcher.EnableUpdateComponent(this);

        _animiationController.ResumeAnimation();
    }

    private void OnGamePause()
    {
        _switcher.DisableUpdateComponent(_input);
        _switcher.DisableUpdateComponent(this);

        _animiationController.PauseAnimation();
    }

    private void OnPlayerJump()
    {
        _simulation.Jump();

        if (_animiationController.IsSliding())
            _isBreakAccelerator = true;

        _animiationController.EndSlide();
        _animiationController.PlayJumpAnimation();
    }

    private void OnPlayerRoll()
    {
        _simulation.Roll();
        _animiationController.PlayRollAnimation();
    }

    private void OnPlayerHitObstacle()
    {
        _simulation.HitByObstacle();
        _animiationController.PlayHitObstacleAnimation();
    }

    private void OnPlayerDrowned(object waterSurfacePosition)
    {
        _switcher.DisableUpdateComponent(this);
        
        _simulation.RunStop();
        _animiationController.PlayDrownAnimation(waterSurfacePosition);
    }

    private void OnClickPlay()
    {
        _animiationController.PlayToRunAnimation();
    }

    private void OnClickMenu()
    {
        _switcher.DisableUpdateComponent(_input);
        _switcher.DisableUpdateComponent(this);

        _animiationController.ResumeAnimation();
        _animiationController.ResetAnimation();

        _simulation.MoveToStartPosition();
    }
}
