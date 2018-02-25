using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimeRx;
using UniRx;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

  public static GameManager Instance;
  public GameObject prefecturePrefab;
  public Canvas canvas;
  public Material prefectureMaterial;
  public Text questionText;
  public Text timerText;
  public Text lapText;
  public List<AudioClip> auSuccess = new List<AudioClip>();
  public List<AudioClip> auFailure = new List<AudioClip>();
  public Button resetButton;
  public Light directionalLight;
  public GameObject earthGlow;
  
  public float earthHealth;
  private Object[] textures;
  private Vector3 startPos = new Vector3(0f, 80f, 0f);
  private List<GameObject> optionInstances;
  private int question;
  private AudioSource aus;
  private float time;
  private bool isPlaying;

  private string[] prefectures = new string[]
  {
        "北海道",
        "青森県","岩手県","宮城県","秋田県","山形県","福島県",
        "茨城県","栃木県","群馬県","埼玉県","千葉県","東京都","神奈川県",
        "新潟県","富山県","石川県","福井県","山梨県","長野県", "岐阜県","静岡県","愛知県","三重県",
        "滋賀県","京都府","大阪府","兵庫県","奈良県","和歌山県",
        "鳥取県","島根県","岡山県","広島県","山口県",
        "徳島県","香川県","愛媛県","高知県",
        "福岡県","佐賀県","長崎県","熊本県","大分県","宮崎県","鹿児島県","沖縄県"
  };

  private List<int> restPrefectureIndexes;
  //private Object[] regions = new
  //{
  //    0: "北海道",
  //    6: "東北地方",
  //    13: "関東地方",
  //    22: "中部地方",
  //    29: "近畿地方",
  //    34: "中国地方",
  //    38: "四国地方",
  //    46: "九州地方"
  //}

  void Awake()
  {
    Instance = this;
    textures = Resources.LoadAll("jp_prefectures", typeof(Texture2D));
    aus = this.gameObject.GetComponent<AudioSource>();

    init();

    resetButton.onClick.AddListener(init);
  }
  private void init()
  {
    isPlaying = true;
    destroyObjects();
    questionText.text = "";
    timerText.text = "";
    lapText.text = "";
    time = 0;
    earthHealth = 1;
    setColor();

    restPrefectureIndexes = new List<int>();
    int i = 47;
    while (i > 0)
    {
      restPrefectureIndexes.Add(--i);
    }

    CreateQuestion();
  }
  private void setColor()
  {
    directionalLight.color = Color.HSVToRGB(0, 1f - earthHealth, 1f);
    earthGlow.GetComponent<Renderer>().material.SetColor("_TintColor", Color.HSVToRGB(0.6f + (1 - earthHealth) * 0.4f, 1f, 0.6f));
  }
  private void destroyObjects()
  {
    if (optionInstances == null) return;
    foreach (var go in optionInstances)
    {
      go.GetComponent<Button>().onClick.RemoveAllListeners();
      go.SetActive(false);
      Observable.Timer(System.TimeSpan.FromSeconds(1f))
      .Subscribe(_ => Destroy(go));
    }
    optionInstances = null;
  }
  private void Update()
  {
    if (!isPlaying) return;

    time += Time.deltaTime;
    timerText.text = time.ToString("F2");
  }
  void CreateQuestion()
  {
    int optionsCount = Mathf.Min(Random.Range(2, 5), restPrefectureIndexes.Count);
    float span = 200f;
    float left = (optionsCount - 1) * -span * 0.5f;
 
    var tempList = new List<int>(restPrefectureIndexes);

    List<int> options = new List<int>();

    for (int i = 0; i < optionsCount; i++)
    {
      int pick = Random.Range(0, tempList.Count);
      options.Add(tempList[pick]);
      tempList.RemoveAt(pick);
    }

    question = options[Random.Range(0, options.Count)];
//    Debug.Log(question);
    questionText.text = prefectures[question];

    optionInstances = new List<GameObject>();

    for(var i = 0; i < options.Count; i++) {

      GameObject go = CreateCard(options[i]);
      optionInstances.Add(go);
      go.SetActive(false);
      Vector3 scale = go.transform.localScale;

      Observable
      .Return(i)
      .Delay(System.TimeSpan.FromSeconds(Random.Range(0f, 0.5f)))
      .Subscribe(index =>
      {
        go.SetActive(true);
        Anime.Play(
          startPos,
          new Vector3(left + span * index, 0f, 0f),
          Easing.InExpo(System.TimeSpan.FromSeconds(1.0f))
        ).SubscribeToLocalPosition(go);

        Anime.Play(
          scale * 0.01f,
          scale,
          Easing.InExpo(System.TimeSpan.FromSeconds(1.0f))
        ).SubscribeToLocalScale(go);
      }
      );

    }
  }
  GameObject CreateCard(int index)
  {
//    Debug.Log(index);
    Texture2D texture = (Texture2D)textures[index];
    GameObject go = Instantiate(prefecturePrefab);

    var mtr = new Material(prefectureMaterial);
    mtr.mainTexture = texture;
    go.GetComponent<Image>().material = mtr;
    float ratio = (float)texture.width / texture.height;

    go.transform.localScale = (ratio > 1
        ? new Vector3(1, 1 / ratio, 1)
        : new Vector3(ratio, 1, 1));

    go.name = index.ToString();
    go.transform.SetParent(canvas.transform);

    go.GetComponent<Button>().onClick.AddListener(() => onSelectPrefecture(index));
    return go;

  }
  public void onSelectPrefecture(int index)
  {
    if (question == index)
    {
      restPrefectureIndexes.Remove(question);
      aus.clip = auSuccess[Random.Range(0, auSuccess.Count)];
      aus.Play();

      lapText.text = lapText.text + timerText.text + " " + prefectures[index] + "\n";
      earthHealth = Mathf.Min(1, earthHealth + Random.Range(0.01f, 0.02f));

    }
    else
    {
      aus.clip = auFailure[Random.Range(0, auFailure.Count)];
      aus.Play();
      earthHealth = Mathf.Max(0, earthHealth - Random.Range(0.2f, 0.4f));
    }
    setColor();
    destroyObjects();

    if(earthHealth == 0)
    {
      questionText.text = "地 球 滅 亡";
      isPlaying = false;
    }
    else if (restPrefectureIndexes.Count == 0)
    {
      questionText.text = "都道府県マスター！";
      isPlaying = false;
    }
    else
    {
      CreateQuestion();
    }

  }
}

