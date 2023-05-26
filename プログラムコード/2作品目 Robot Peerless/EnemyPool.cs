using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    //オブジェクトプールのオブジェクト(自身)のTrabform
    private Transform _poolTransform;
    //生成する敵オブジェクトのプレハブ
    [SerializeField] GameObject _enemyPrefab = null;

    [Header("敵の数の情報")]
    //マップ上にいる敵の数
    [SerializeField]
    private int _enemyCount;
    //マップ上の敵の最大数
    [SerializeField]
    private int _enemyMaxCount = 200;
    //敵リスポーン場所
    private GameObject[] _respawn;
    //敵リスポーンが出来るか(trueは出来る)
    private bool[] _isEnemyRespawn = { true, true, true, true };

    void Start()
    {
        //オブジェクトプールのTransformを取得
        _poolTransform = this.transform;
        //リスポーン地点をタグで検索配列に格納
        try
        {
            _respawn = GameObject.FindGameObjectsWithTag("Respawn");
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError("リスポーン場所が存在しません。");
        }
        //開始時にマップ上にいる敵の数を取得
        try
        {
            _enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
        }
        catch (System.NullReferenceException e)
        {
            //一体も敵がいなかった場合は0とする
            _enemyCount = 0;
        }
       
    }

    private void Update()
    {
        //敵の数が_enemyMaxCountより少ない場合敵を出す
        if (_enemyCount < _enemyMaxCount)
        {
            //敵のレイヤーにだけ当たるように指定
            int layerMask = 1 << 8;
            //リスポーン場所全てにRayを飛ばす
            for (int i = 0; i < _respawn.Length; i++)
            {
                //Rayni当たったオブジェクト
                RaycastHit hit;
                //リスポーン場所のpositionを取得
                Vector3 respwanPosition = _respawn[i].transform.position;
                //リスポーン場所からRayを飛ばすと低いので少し上げる
                respwanPosition.y += 3.0f;
                //リスポーン場所からRayを飛ばし敵にぶつかった
                if (Physics.Raycast(respwanPosition, _respawn[i].transform.up * -1, out hit, 10.0f, layerMask))
                {
                    //ぶつかった場合敵が被るのでそのリスポーン場所はリスポーン出来ないのでfalseにする
                    _isEnemyRespawn[i] = false;
                }
                else
                {
                    //ぶつからない場合はリスポーン出来るのでtrue
                    _isEnemyRespawn[i] = true;
                }
            }
            //0～リスポーン場所の数の数値をランダムで生成
            int rnd = Random.Range(0, _respawn.Length);
            //ランダムで生成したリスポーン場所がtrueだった場合は敵を生成
            if (_isEnemyRespawn[rnd] == true)
            {
                InstEnemy(_respawn[rnd].transform.position, transform.rotation);
                _enemyCount++; ;
            }
        }
    }
    //敵を生成するかtrueにするか
    void InstEnemy(Vector3 pos, Quaternion rotation)
    {
        //アクティブでないオブジェクトを子の中から探索
        foreach (Transform t in _poolTransform)
        {
            if (!t.gameObject.activeSelf)
            {
                //非アクティブなオブジェクトの位置と回転を設定
                t.SetPositionAndRotation(pos, rotation);
                //アクティブにする
                t.gameObject.SetActive(true);
                return;
            }
        }
        //非アクティブなオブジェクトがない場合新規生成
        //子オブジェクトとして生成する
        Instantiate(_enemyPrefab, pos, rotation, _poolTransform);
    }

    //敵がやられたか分を計測する
    public void EnemyStateSearch()
    {
        //activeがfalseになっている敵を数える
        for (int k = 0; k < this.transform.childCount; k++)
        {
            if (this.transform.GetChild(k).gameObject.activeSelf == false)
            {
                //false分を減らす
                _enemyCount--;
            }
        }
    }
}

