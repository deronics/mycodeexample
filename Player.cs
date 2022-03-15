using UnityEngine;

public class Player : EntityUF
{
    [SerializeField] private CoroutineExecutor _coroutineExecutor;
    [SerializeField] private PlayerInput _input;                                // Модуль управление игрока
    [SerializeField] private PlayerSimulation _simulation;                      // Модуль физики игрока
    [SerializeField] private PlayerAnimationController _animationController;    // Модуль анимации игрока

    private PlayerPlatformAnimationController _platformAnimationController;
    private PlayerLogic _logic;                                					// Модуль логики игрока
    private PlayerCollisionHandler _collisionHandler;          					// Модуль отвечающий за столкновения с объектами

#region UnityLifeCycle
    
    protected override void Awake()
    {
        _platformAnimationController = new PlayerPlatformAnimationController();
        _logic = new PlayerLogic();
        _collisionHandler = new PlayerCollisionHandler();
        
        Initialize(_logic, _simulation, _input, _collisionHandler, _animationController, _platformAnimationController);

        _input.InitializeComponent(_coroutineExecutor);
        _platformAnimationController.InitializeComponent(_coroutineExecutor);
        _animationController.InitializeComponent(_coroutineExecutor);
        _simulation.InitializeComponent(_coroutineExecutor);
        _logic.InitializeSwitcher(this, _coroutineExecutor);
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        _logic = null;
        _input = null;
        _simulation = null;
        _animationController = null;
        _collisionHandler = null;
    }

#endregion UnityLifeCycle

    // Все столкновения обрабатывает класс PlayerCollisionHandler 
    private void OnTriggerEnter(Collider other) =>
        _collisionHandler.OnTriggerEnter(transform, other);

    private void OnTriggerExit(Collider other) =>
        _collisionHandler.OnTriggerExit(other);

    private void OnControllerColliderHit(ControllerColliderHit hit) =>
        _collisionHandler.OnControllerColliderHit(hit);
}
