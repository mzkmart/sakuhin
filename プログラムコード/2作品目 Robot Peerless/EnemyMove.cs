using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    #region 敵関係の設定
    //敵の移動のスピード
    const float _ENEMY_SPEED = 2.0f;
    //敵の攻撃ステートになる距離
    const float _CHECK_DISTANCE = 2.0f;
    //敵の攻撃出来るかの判定
    private bool _isAttackCheck = true;
    //敵の攻撃の攻撃の判定までの間隔
    const float _attackCheck = 0.8f;
    //敵の攻撃の当たる距離
    const float _ATTACK_RANGE = 4.0f;
    //敵のステートの種類
    private enum State
    {
        Idel,
        Move,
        Attack,
    }
    //敵のステート
    private State _enemyState;
    //敵のキャラクターコントローラー
    private CharacterController _controller;

    //敵の攻撃時のエフェクト
    private GameObject _attackEffect;

    //敵のアニメーター
    private Animator _animator;
    #endregion

    #region プレイヤー関係の設定
    //プレイヤー
    private GameObject _player;
    //プレイヤーのスクリプト
    private PlayerController _playerController;
    //プレイヤーと敵の座標の差
    private Vector3 _direction;
    #endregion

    void OnEnable()
    {
        //敵のアニメーターを取得
        try
        {
            _animator = GetComponent<Animator>();
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError("敵のアニメーターが存在しません。");
        }
        //敵のキャラクターコントローラーを取得
        try
        {
            _controller = GetComponent<CharacterController>();
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError("敵のキャラクターコントローラーが存在しません。");
        }
        //敵の攻撃時のエフェクトを取得する
        _attackEffect = transform.GetChild(0).gameObject;
        //敵のステートをIdelでスタートさせる
        _enemyState = State.Idel;

        //プレイヤーを取得
        try
        {
            _player = GameObject.Find("Player");
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError("プレイヤーが存在しません。");
        }
        //プレイヤーのスクリプトを取得
        try
        {
            _playerController = _player.GetComponent<PlayerController>();
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError("プレイヤーのスクリプトが存在しません。");
        }
        
    }

    private void Update()
    {
        //プレイヤーと敵の座標の差を求める
        _direction = _player.transform.position - this.transform.position;
        //Y座標は固定にしたいので0にする
        _direction.y = 0.0f;
        // 座標の差からQuaternion(回転値)を取得
        Quaternion quaternion = Quaternion.LookRotation(_direction);
        //敵をプレイヤーの方向に向ける
        this.transform.rotation = quaternion;
    }

    private void FixedUpdate()
    {
        //敵のステートに合わせてSwitch文を走らせる
        switch (_enemyState)
        {
            //Idleの場合
            case State.Idel:
                //スポーンのアニメーションが終わっていたらステートをMoveにする
                if (_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "Dash Forward In Place")
                {
                    _enemyState = State.Move;
                }
                break;
            case State.Move:
                //アニメーションのwalkFlagをtrue
                _animator.SetBool("walkFlag", true);
                //アニメーションのattackFlagをfalse
                _animator.SetBool("attackFlag", false);
                //敵を向かっている方向に進める
                _controller.Move(transform.forward * _ENEMY_SPEED * Time.deltaTime);
                //プレイヤーとの差が無くなったらステートをAttackにする
                if ((_direction.x < _CHECK_DISTANCE && _direction.x > _CHECK_DISTANCE * -1) &&
                    (_direction.z < _CHECK_DISTANCE && _direction.z > _CHECK_DISTANCE * -1))
                {
                    _enemyState = State.Attack;
                }
                break;
            case State.Attack:
                //アニメーションのwalkFlagをfalse
                _animator.SetBool("walkFlag", false);
                //アニメーションのattackFlagをtrue
                _animator.SetBool("attackFlag", true);
                //一度の攻撃モーションが終わるまでに一度だけ
                if (_isAttackCheck)
                {
                    //攻撃モーションのタイミングに合わせて攻撃の判定を取る
                    Invoke(nameof(EnemyAttack), _attackCheck);
                    //攻撃のモーションが終わるまでfalse
                    _isAttackCheck = false;
                }
                //プレイヤーとの距離が離れた場合はステートをMoveにする
                if (!(_direction.x < _CHECK_DISTANCE && _direction.x > _CHECK_DISTANCE * -1) &&
                    !(_direction.z < _CHECK_DISTANCE && _direction.z > _CHECK_DISTANCE * -1))
                {
                    _enemyState = State.Move;
                }
                break;
        }
    }

    private  void EnemyAttack()
    {
        //敵の座標を取得
        Vector3 vector3 = transform.position;
        //プレイヤーに当たるように下げる
        vector3.y = 0f;
        //Raycaastに当たったプレイヤーの情報を入れる変数を宣言
        RaycastHit hit;
        //Raycastをプレイヤーにしか当たらないようにレイヤーを宣言
        int layerMask = 1 << 9;
        //後で消す　　Raycastの視覚化
        Debug.DrawRay(vector3, this.transform.forward, Color.blue, _ATTACK_RANGE);
        //敵が向いてるほうにRayを飛ばしプレイヤーにぶつかったか
        if (Physics.Raycast(vector3, this.transform.forward, out hit, _ATTACK_RANGE, layerMask))
        {
            //攻撃が当たったのでエフェクトを出す
            _attackEffect.SetActive(true);
            //ぶつかった場合はプレイヤーのライフを1減らす
            _playerController.Damage();
        }
        //攻撃のモーションが終わるからtrue
        _isAttackCheck = true;
    }
}
