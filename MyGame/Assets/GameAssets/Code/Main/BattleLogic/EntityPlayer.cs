using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPlayer : MonoBehaviour
{
    public RoomBoundary Boundary;
    public float MoveSpeed = 10f;
    public float FireRate = 0.25f;

    private float _nextFireTime = 0f;
    private Transform _shotSpawn;
    private Rigidbody _rigidbody;
    private AudioSource _audioSource;

    void Awake()
    {
        _rigidbody = this.gameObject.GetComponent<Rigidbody>();
        _audioSource = this.gameObject.GetComponent<AudioSource>();
        _shotSpawn = this.transform.Find("shot_spawn");
    }
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetButton("Fire1") && Time.time > _nextFireTime)
#else
        if (Time.time > _nextFireTime)
#endif
        {
            _nextFireTime = Time.time + FireRate;
            _audioSource.Play();
            BattleEventDefine.PlayerFireBullet.SendEventMessage(_shotSpawn.position, _shotSpawn.rotation);
        }
    }
    void FixedUpdate()
    {
#if UNITY_EDITOR
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        _rigidbody.velocity = movement * MoveSpeed;
        _rigidbody.position = new Vector3
        (
            Mathf.Clamp(GetComponent<Rigidbody>().position.x, Boundary.xMin, Boundary.xMax),
            0.0f,
            Mathf.Clamp(GetComponent<Rigidbody>().position.z, Boundary.zMin, Boundary.zMax)
        );

        float tilt = 5f;
        _rigidbody.rotation = Quaternion.Euler(0.0f, 0.0f, _rigidbody.velocity.x * -tilt);
#else
        HandleTouchInput();
#endif
    }

    public float moveSpeed = 0.5f; // 移动速度
    private Vector2 touchStartPos; // 触摸开始的位置
    private Vector2 touchCurrentPos; // 当前触摸的位置
    private bool isDragging = false; // 是否正在拖拽
    void HandleTouchInput()
    {
        if (Input.touchCount > 0) // 检查是否有触摸输入
        {
            Touch touch = Input.GetTouch(0); // 获取第一个触摸点

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    isDragging = true;
                    break;

                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        touchCurrentPos = touch.position;
                        Vector2 delta = touchCurrentPos - touchStartPos;
                        MoveObject(delta);
                        touchStartPos = touchCurrentPos;
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isDragging = false;
                    break;
            }
        }
    }

    void MoveObject(Vector2 delta)
    {
        // 将屏幕坐标的移动转换为世界坐标的移动
        Vector3 movement = new Vector3(delta.x, 0, delta.y) * moveSpeed * Time.deltaTime;
        _rigidbody.transform.Translate(movement);
    }
    void OnTriggerEnter(Collider other)
    {
        var name = other.gameObject.name;
        if (name.StartsWith("enemy") || name.StartsWith("asteroid"))
        {
            BattleEventDefine.PlayerDead.SendEventMessage(this.transform.position, this.transform.rotation);
            GameObject.Destroy(this.gameObject);
        }
    }
}