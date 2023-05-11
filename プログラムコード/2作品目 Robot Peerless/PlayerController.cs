using UnityEngine;
using UnityEngine.UI;

//プレイヤーの制御クラス
public class PlayerController : MonoBehaviour
{
    #region ﾌﾟﾚｲﾔｰ関係の設定
    //プレイヤーの歩くスピード
    const float _WALK_SPEED = 3.0f;
    //プレイヤーの走るスピード
    const float _RUN_SPEED = 5.0f;
    //走りに代わるスティックの傾き
    const float _RUN_STICK_SLOP = 0.7f;
    //プレイヤーのライフ
    private float _life;
    //プレイヤーのMAXライフ
    const float _LIFEMAX = 250.0f;
    //プレイヤーの攻撃の範囲
    const float _ATTACK_RANGE = 1.0f;
    //プレイヤーの攻撃の当たる距離
    const float _ATTACK_DISTANCE = 2.0f;
    //プレイヤーの攻撃出来るかの判定
    private bool _isAttackCheck = true;
    //プレイヤーの攻撃までの間隔
    const float _attackCheck = 0.5f;
    //プレイヤーの攻撃アニメーションの終了時間
    const float _attackAnimationEnd = 1.0f;

    //プレイヤーのステートの種類
    private enum State
    {
        Idel,
        Walk,
        Run,
        Attack,
        Die
    }
    //プレイヤーのステート
    State _playerState;

    //プレイヤーのアニメーター
    private Animator _animator;
    //プレイヤーのキャラクターコントローラー
    private CharacterController _controller;

    //左ステックの縦の傾き
    private float _verticalValue;
    //左スティックの横の傾き
    private float _horizontalValue;
    #endregion

    #region キャンバス関係の設定
    [Header("キャンバスの設定項目")]
    //残り時間などの常に表示されてるUI
    [SerializeField]
    private GameObject _uiCanvas;
    //プレイヤーのHPバー
    [SerializeField]
    private Slider _lifeSlider;
    //残り時間のテキスト
    [SerializeField]
    private Text _remainingTimeText;
    [Header("終了後のキャンバスの設定項目")]
    //終了キャンバス
    [SerializeField]
    private GameObject _endCanvas;
    //倒した敵の数のテキスト
    [SerializeField]
    private Text _enemyDefeatedCountText;
    #endregion

    #region エフェクト関係の設定
    //エフェクトのオブジェクトプール
    private GameObject _effectPoolObject;
    //エフェクトのオブジェクトプールのスクリプト
    private EffectPool _effectPool;
    #endregion

    #region　敵関係の設定
    //敵のオブジェクトプールのスクリプト
    EnemyPool _enmyPool;
    //敵のレイヤーの番号
    const int _ENEMY_LAYER = 1 << 8;
    #endregion

    #region オーディオ関係の設定
    [Header("オーディオの設定項目")]
    //プレイヤーのオーディオソース
    [SerializeField]
    private AudioSource _audioSource;
    //プレイヤーの攻撃のSE
    [SerializeField]
    private AudioClip _attackSE;
    //プレイヤーがダメージを受けた時のSE
    [SerializeField]
    private AudioClip _damageSE;
    #endregion

    #region string型の設定
    //カメラの名前
    const string _CAMERA_NAME = "MainCamera";
    //敵のオブジェクトプールの名前
    const string _ENEMY_POOL_NAME = "EnemyPool";
    //エフェクトのオブジェクトプールの名前
    const string _EFFECT_POOL_NAME = "EffectPool";
    //攻撃ボタンの名前
    const string _ATTACK_BUTTON_NAME = "Fire1";
    #endregion

    //カメラ
    private GameObject _camera;

    //ゲーム開始から経過した時間
    private float _gameTime;
    //ゲームの制限時間
    const float _LIMIT_TIME = 60.0f;

    //敵を倒した数
    private int _enemyDefeatedCount;


    void Start()
    {
        //プレイヤーのアニメーター取得
        try
        {
            _animator = GetComponent<Animator>();
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError("アニメーターが存在しません。");
        }
        //プレイヤーのキャラクターコントローラー取得
        try
        {
            _controller = GetComponent<CharacterController>();
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError("キャラクターコントローラーが存在しません。");
        }
        //プレイヤーのステートをIdleでスタートさせる
        _playerState = State.Idel;

        //メインカメラの取得
        try
        {
            _camera = GameObject.Find(_CAMERA_NAME);
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError("カメラが存在しません。");
        }

        //敵のプールのスクリプト取得
        try
        {
            _enmyPool = GameObject.Find(_ENEMY_POOL_NAME).GetComponent<EnemyPool>();
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError("敵のオブジェクトプールが存在しません。");
        }

        //エフェクトのプールのオブジェクトとスクリプト取得
        try
        {
            _effectPoolObject = GameObject.Find(_EFFECT_POOL_NAME);
            _effectPool = _effectPoolObject.GetComponent<EffectPool>();
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError("エフェクトのオブジェクトプールが存在しません。");
        }

        //経過時間を0に
        _gameTime = 0.0f;
        //倒した敵の数を0に
        _enemyDefeatedCount = 0;
        //プレイヤーのライフをMaxにする
        _life = _LIFEMAX;
    }

    private void Update()
    {
        //ゲームの経過時間を計測
        _gameTime += Time.deltaTime;
        //経過時間から残り時間を計算しテキストに代入
        _remainingTimeText.text = string.Format("{0:00}", _LIMIT_TIME - _gameTime);

        //コントローラーのスティックの傾きを取得
        _verticalValue = Input.GetAxis("Vertical");
        _horizontalValue = Input.GetAxis("Horizontal");

        //プレイヤーのライフゲージを更新する
        _lifeSlider.value = _life / _LIFEMAX;

        //プレイヤーのライフが０以下になったか
        if (_life <= 0)
        {
            //プレイヤーのステートをDieにする
            _playerState = State.Die;
            if (_animator.GetInteger("PlayerState") != 4)
                _animator.SetInteger("PlayerState", 4);
            _enemyDefeatedCountText.text = string.Format("{0:0}", _enemyDefeatedCount);
            _uiCanvas.SetActive(false);
            _endCanvas.SetActive(true);
        }
        //経過時間が制限時間を超えたら
        else if (_gameTime >= _LIMIT_TIME)
        {
            _enemyDefeatedCountText.text = string.Format("{0:0}", _enemyDefeatedCount);
            _uiCanvas.SetActive(false);
            _endCanvas.SetActive(true);

        }

        //コントローラーのAボタン、左クリックがされたか
        if (Input.GetButtonDown(_ATTACK_BUTTON_NAME))
        {
            //プレイヤーのステートをAttackにする
            if (_playerState != State.Attack)
                _playerState = State.Attack;
            if (_animator.GetInteger("PlayerState") != 3)
                _animator.SetInteger("PlayerState", 3);
        }
        //プレイヤーのステートが攻撃中じゃないか
        else if (_playerState != State.Attack)
        {
            //コントローラーの左スティックが傾いているか
            if (_verticalValue != 0 || _horizontalValue != 0)
            {
                //コントローラーの傾きが_runStateで決められた以上に傾いているか
                if (_verticalValue > _RUN_STICK_SLOP || _verticalValue < _RUN_STICK_SLOP * -1.0f ||
                    _horizontalValue > _RUN_STICK_SLOP || _horizontalValue < _RUN_STICK_SLOP * -1.0f)
                {
                    //プレイヤーのステートをRunにする
                    if (_playerState != State.Run)
                        _playerState = State.Run;
                    if (_animator.GetInteger("PlayerState") != 2)
                        _animator.SetInteger("PlayerState", 2);
                }
                else
                {
                    //傾きが小さかった場合はプレイヤーのステートをWalkにする
                    if (_playerState != State.Walk)
                    _playerState = State.Walk;
                    if (_animator.GetInteger("PlayerState") != 1)
                        _animator.SetInteger("PlayerState", 1);

                }
            }
            else
            {
                //何も操作されていないか
                if (_playerState != State.Idel)
                    _playerState = State.Idel;
                if (_animator.GetInteger("PlayerState") != 0)
                    _animator.SetInteger("PlayerState", 0);
            }
        }
    }
    private void FixedUpdate()
    {
        //プレイヤーのステートに合わせてSwitch文を走らせる
        switch (_playerState)
        {
            //ステートがIdleの場合はなにもしない
            case State.Idel:
                break;
            //ステートがWalkの場合
            case State.Walk:
                //プレイヤー移動のメソッド呼び出し
                PlayerMove(_WALK_SPEED);
                break;
            //ステートがRunの場合
            case State.Run:
                //プレイヤー移動のメソッド呼び出し
                PlayerMove(_RUN_SPEED);
                break;
            //ステートがAttackの場合
            case State.Attack:
                //ステートがAttackになった最初の一回だけ
                if (_isAttackCheck)
                {
                    //攻撃の判定とエフェクトのメソッドをアニメーションに合わせて呼び出す
                    Invoke(nameof(PlayerAttack), _attackCheck);
                    //アニメーションの終わりに合わせてステートをIdleに戻すメソッドを呼び出す
                    Invoke(nameof(AttackEnd), _attackAnimationEnd);
                    //一度処理したのでfalseにする 
                    _isAttackCheck = false;
                }
                break;
        }
    }

    //プレイヤーの移動のメソッド
    private void PlayerMove(float speed)
    {
        //カメラの向きからプレイヤーの移動の向きを取得
        Quaternion horizontalRotation = Quaternion.AngleAxis(_camera.transform.eulerAngles.y, Vector3.up);
        //カメラとの差だとY軸も回ってしまうが、Y軸は変えないので0に指定
        Vector3 direction = horizontalRotation * new Vector3(_horizontalValue, 0, _verticalValue).normalized;
        //コントローラーの傾きとカメラの向きからプレイヤーの横に回転させる
        transform.rotation = Quaternion.LookRotation(direction);
        //プレイヤーの傾きは上記で決まったのでその向きに進める
        _controller.Move(transform.forward * speed * Time.deltaTime);

    }

    //プレイヤーの攻撃メソッド
    private void PlayerAttack()
    {
        _audioSource.PlayOneShot(_attackSE);
        _effectPool.InstEfect(_effectPoolObject.transform.position,_effectPoolObject.transform.rotation);
        //プレイヤーの座標を取得
        Vector3 attackPosition = transform.position;
        //プレイヤーのY座標では低いので上げる
        attackPosition.y = 1.0f;
        //SphereCastに当たった敵の情報を配列に全て入れる
        RaycastHit[] hits = Physics.SphereCastAll(attackPosition, _ATTACK_RANGE, this.transform.forward, _ATTACK_DISTANCE, _ENEMY_LAYER);
        //SphereCastに当たった敵の分for文を回す
        for (int i = 0; i < hits.Length; i++)
        {
            //当たったオブジェクトのSetActiveをfalseにする
            hits[i].collider.gameObject.SetActive(false);
            _enemyDefeatedCount++;
        }
        //攻撃が敵に一体でも当たっていたら
        if (hits.Length > 0)
        {
            //オブジェクトプールに返す
            _enmyPool.EnemyStateSearch();
        }
    }

    //プレイヤーの攻撃のアニメーションが終わったら呼び出される
    private void AttackEnd()
    {
        //プレイヤーのステートをIdelにする
        _playerState = State.Idel;
        //攻撃の判定を取れるようにtrueに戻す
        _isAttackCheck = true;
    }

    public void Damage()
    {
        _audioSource.PlayOneShot(_damageSE);
        _life--;
    }
}