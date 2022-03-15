using System.Collections;
using UnityEngine;

// Отвечает за физику движения игрока
[System.Serializable]
public class PlayerSimulation : IEntityComponent, IInitializableComponent
{
    [SerializeField] private float _runSpeed = 4.2f;                    // Скорость бега игрока
    [SerializeField] private float _runSpeedSlow = 2f;                  // Скорость бега игрока в слизи
    [SerializeField] private float _runSpeedFast = 8f;                  // Скорость бега игрока на конвеере
    [SerializeField] private float _jumpHeightInUnits = 1.5f;           // Высотая прыжка игрока
    [SerializeField] private Transform _playerTransform;                // Transform игрока
    [SerializeField] private LayerMask _solidSurfaces;                  // Поверхности, на которых может стоять игрок
    [SerializeField] private Transform _frontEdgeCheck;                 // Передний чекер соскальзывания
    [SerializeField] private Transform _backEdgeCheck;                  // Задний чекер соскальзывания
    [SerializeField] private CharacterController _controller;           // Физика игрока
    
    private float _velocityYBeforeGrounded = 0f;
    private float _jumpSqrt;                        // Вычисленное ускорение необходимое для прыжка определённой высоты
    private float _gravity;                         // Скэшированная величина глобальной гравитации
    private float _preJumpDuration = 0.15f;         // Время ожидания приземления, при раннем прыжке
    private float _slidingSpeedFromEdge = 1f;       // Скорость соскальзывания с края
    private float _raycastCheckLength = 0.06f;      // Длина испускаемого луча для проверки земли
    private Vector3 _velocity;                      // Скорость игрока
    private Vector3 _checkBoxHalfExtents;           // Половина размера бокс рэйкаста isGrounded
    private Vector3 _positionSliding;               // Позиция при соскальзывания с края
    private Vector3 _startPosition;
    
    public bool IsGrounded { get; private set; }
    public float VelocityY => _velocity.y;
    public float VelocityYBeforeGrounded => _velocityYBeforeGrounded;
    private CoroutineExecutor _coroutineExecutor;
    public Transform PlayerTransform => _playerTransform;

    public void Initialize(IEntityComponentAdapter entityComponentAdapter)
    {
        // Придаем начальное ускорение
        _velocity = Vector3.down;

        // Берём глобальную гравитацию
        _gravity = Physics.gravity.y;

        // Вычисляем ускорение для прыжка определённой высоты
        _jumpSqrt = Mathf.Sqrt(_jumpHeightInUnits * -2f * _gravity);

        // Настраиваем габариты рэйкаста для проверки земли
        _checkBoxHalfExtents = new Vector3(0.125f, (_raycastCheckLength + 0.04f) / 2f, 0.45f);
        
        _startPosition = _playerTransform.position;
    }

    public void InitializeComponent(CoroutineExecutor coroutineExecutor)
    {
        _coroutineExecutor = coroutineExecutor;
    }

    /// <summary>
    /// Обновляет физику движения игрока. Вызывать в методе FixedUpdate
    /// </summary>
    public void UpdatePhysics()
    {
        // Проверяем находится ли игрок на земле
        CheckIsGrounded();

        if (IsGrounded)
        {
            // Запоминаем ускорение после приземления (до изменения вертикального ускорения)
            if (_velocity.y != -1f)
                _velocityYBeforeGrounded = _velocity.y;

            // Находясь на земле, вертикальное ускорение равно -1
            if (_velocity.y < -1f)
                _velocity.y = -1f;
        }
        else
        {
            _velocityYBeforeGrounded = 0f;
            
            // Находясь на воздухе - добавляем силу притяжения
            _velocity.y = Mathf.Clamp((_gravity * Time.fixedDeltaTime) + _velocity.y, _gravity, -_gravity);

            // Проверяем находится ли игрок на краю платформы
            TrySlideFromEdge();
        }
    }

    /// <summary>
    /// Двигает игрока. Вызывать в методе Update
    /// </summary>
    public void Move()
    {
        _controller.Move(_velocity * Time.deltaTime);
    }

    public void MoveToStartPosition()
    {
        _velocity = Vector3.down;
        _controller.enabled = false;
        _playerTransform.position = _startPosition;
        _controller.enabled = true;
    }

    public void Jump()
    {
        // Если игрок находится на земле - сразу прыгаем
        if (IsGrounded)
            AddForceToJump();
        else
        // Иначе ждем _preJumpDuration сек. и пробуем прыгнуть ещё раз, проверив IsGrounded
            _coroutineExecutor.StartMyCoroutine(WaitPreJump());
    }

    public void Roll()
    {
        if (!IsGrounded)
            _velocity.y = -_jumpSqrt;
    }

    public void RunForward() => _velocity.x = _runSpeed;

    public void RunOnSlime() => _velocity.x = _runSpeedSlow;
    
    public void RunOnAccelerator() => _velocity.x = _runSpeedFast;

    public void RunStop() => _velocity.x = 0f;

    public void HitByObstacle()
    {
        RunStop();
        _velocity.y = -_jumpSqrt * 0.25f;
    }

    public void ResetVelocityBeforeGrounded()
    {
        _velocityYBeforeGrounded = 0f;
    }

    // Проверяет находится ли игрок на земле
    private void CheckIsGrounded()
    {
        IsGrounded = Physics.CheckBox(_controller.transform.position, _checkBoxHalfExtents, Quaternion.identity, _solidSurfaces);
    }

    // Если игрок находится на краю земли, то он начинает соскальзывать
    private void TrySlideFromEdge()
    {
        bool frontEdge = Physics.Raycast(_frontEdgeCheck.position, Vector3.down, _frontEdgeCheck.localPosition.z + _raycastCheckLength, _solidSurfaces, QueryTriggerInteraction.Ignore);
        bool backEdge = Physics.Raycast(_backEdgeCheck.position, Vector3.down, Mathf.Abs(_backEdgeCheck.localPosition.z) + _raycastCheckLength, _solidSurfaces, QueryTriggerInteraction.Ignore);

        if (frontEdge && backEdge) return;
        if (backEdge)
        {
            _positionSliding = _playerTransform.position;
            _positionSliding.x += _slidingSpeedFromEdge * Time.deltaTime;
            _playerTransform.position = _positionSliding;
        }
        else if (frontEdge)
        {
            _positionSliding = _playerTransform.position;
            _positionSliding.x -= _slidingSpeedFromEdge * Time.deltaTime;
            _playerTransform.position = _positionSliding;
        }
    }

    // Придает ускорение для прыжка
    private void AddForceToJump() => _velocity.y = _jumpSqrt;

    // Если игрок нажал на прыжок чуть раньше, не находясь на земле
    private IEnumerator WaitPreJump()
    {
        float elapsedTime = 0f;
        while (elapsedTime < _preJumpDuration)
        {
            elapsedTime += Time.deltaTime;

            if (IsGrounded)
            {
                AddForceToJump();
                break;
            }

            yield return null;
        }
    }
}
