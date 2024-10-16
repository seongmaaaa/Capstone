using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakePlusLine : MonoBehaviour
{
    // <SnakeManager>
    [SerializeField] float distanceBetween = 0.2f;  // 몸체 생성 시간차 (신체 부위 간격)
    [SerializeField] float speed = 280;     // 이동 속도
    [SerializeField] float turnSpeed = 180;  // 회전 속도
    [SerializeField] List<GameObject> bodyParts = new List<GameObject>();   // 생성할 몸체 게임오브젝트(프리팹) 리스트
    [SerializeField] List<GameObject> snakeBody = new List<GameObject>();   // 생성된 몸체 게임오브젝트 리스트

    [SerializeField] GameObject[] bodyPartsDynamic;   // 동적으로 추가될 몸체 게임오브젝트(프리펩) 배열

    float countUp = 0;  // 몸체 생성을 위한 누적 시간

    // <LineRenderer>
    Rigidbody2D rigid;

    [SerializeField] LineRenderer lineRenderer;     // 경로를 시각적으로 표시할 라인 렌더러
    [SerializeField] List<Vector2> points = new List<Vector2>();  // 플레이어가 지나간 경로 좌표 저장
    public int maxPoints = 200;  // 좌표의 개수를 제한
    bool trailClosed = false;     // 경로가 닫혔는지 여부 확인
    [SerializeField] float closeDistance = 0.5f;    // 경로 닫기 감지 거리
    [SerializeField] int minimumPoints = 3;          // 경로가 닫히기 위해 필요한 최소 점 개수
    [SerializeField] float minimumPathLength = 1.0f; // 경로가 닫히기 위한 최소 경로 길이 (지정하지 않으면 라인이 바로 지워짐)

    public PolygonCollider2D polygonCollider;           // 경로가 닫힐 때 사용될 콜라이더 (월드 좌표 기준으로 콜라이더를 생성하기 위해서 DestroyZone의 컴포넌트 참조)

    void Awake()
    {
        CreateBodyParts(); 

        rigid = GetComponent<Rigidbody2D>();
        lineRenderer = GetComponentInChildren<LineRenderer>();

        lineRenderer.positionCount = 0;    // 라인 렌더러 초기화
    }

    void Update()
    {
        if (Input.GetKeyDown("1"))
            AddBodyParts(bodyPartsDynamic[0]);
        if (Input.GetKeyDown("2"))
            AddBodyParts(bodyPartsDynamic[1]);
        if (Input.GetKeyDown("3"))
            AddBodyParts(bodyPartsDynamic[2]);
        if (Input.GetKeyDown("4"))
            AddBodyParts(bodyPartsDynamic[3]);
    }
    void FixedUpdate()
    {
        ManageSnakeBody();
        SnakeMovement();    // FixedUpdate() 내에서 Time.deltaTime을 참조해도 자동으로 Time.fixedDeltaTime의 값이 참조 됨

        // 경로가 닫혔다면 더 이상 경로를 그리지 않음
        if (trailClosed) 
            return;

        Vector2 currentPos = snakeBody[0].transform.position;

        // 플레이어가 이동할 때 경로 추가
        if (points.Count == 0 || Vector2.Distance(currentPos, points[points.Count - 1]) > 0.1f)
        {
            points.Add(currentPos);
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPosition(points.Count - 1, currentPos);
        }

        // 경로를 남길 최대 개수 지정 (@@@ 일정 시간이 지나면 제거하도록 변경, 한번에 제거가 아니라 오래된 좌표부터 서서히 제거되도록 변경 @@@, TimedStamp 클래스 쓸때 기존 points 변환방법)
        if (points.Count > maxPoints)
        {
            //points.RemoveAt(0);  // 가장 오래된 좌표 제거 (0번 인덱스)
            ClearTrail();
        }

        // 최소 경로 길이를 만족하고 경로가 닫혔는지 감지 (첫 번째 점과 현재 위치의 거리가 일정 이하일 때)
        if (points.Count >= minimumPoints && CalculatePathLength() > minimumPathLength && Vector2.Distance(currentPos, points[0]) < closeDistance)
        {
            CloseTrail();
        }
    }

    void ManageSnakeBody()
    {
        if (bodyParts.Count > 0)    // bodyParts 리스트의 요소 개수(=길이, 크기)
        {
            CreateBodyParts();      // 생성할 몸체가 남아있을때 실행
        }

        for (int i = 0; i < snakeBody.Count; i++)   
        {
            if (snakeBody[i] == null)       // snakeBody 리스트의 i번째 몸체 오브젝트가 NULL이면 (몸체가 파괴된 경우)
            {
                snakeBody.RemoveAt(i);      // 해당 인덱스 제거
                i--;
            }
        }
        if (snakeBody.Count == 0)           
        {
            Destroy(this);                  // 몸체 오브젝트가 한개도 존재하지 않으면 파괴
        }
    }

    void CreateBodyParts()
    {
        if (snakeBody.Count == 0)   // 아직 생성된 몸체(머리)가 없다면
        {
            GameObject temp = Instantiate(bodyParts[0], transform.position, transform.rotation, transform); // 머리 생성
            if (!temp.GetComponent<MarkerManager>())
                temp.AddComponent<MarkerManager>();     // MarkerManger 컴포넌트 동적으로 추가
            if (!temp.GetComponent<Rigidbody2D>())
            {
                temp.AddComponent<Rigidbody2D>();       // RigidBody2D 컴포넌트 동적으로 추가
                temp.GetComponent<Rigidbody2D>().gravityScale = 0;  // 중력 0
            }
            snakeBody.Add(temp);    // snakeBody 리스트에 머리 추가
            bodyParts.RemoveAt(0);  // 이미 생성한 몸체는 bodyParts 리스트에서 제거 (RemoveAt(0) : 인덱스로 첫번째 요소 제거) (리스트는 요소가 삭제되면 앞으로 밀림)
        }

        MarkerManager markM = snakeBody[snakeBody.Count - 1].GetComponent<MarkerManager>();     // markM : 마지막으로 생성된 몸체의 MarkerManger
        if (countUp == 0)
        {
            markM.ClearMarkerList();    // markM 마커 리스트를 초기화
        }
        countUp += Time.deltaTime;

        if (countUp >= distanceBetween) // 일정 시간 마다 새로운 몸체 추가
        {
            GameObject temp = Instantiate(bodyParts[0], markM.markerList[0].position, markM.markerList[0].rotation, transform); // 몸통 생성 (MarkerManager의 첫 번째 마커를 참조, 마지막으로 생성된 몸체의 처음 생성된 위치를 참조)
            if (!temp.GetComponent<MarkerManager>())
                temp.AddComponent<MarkerManager>();     
            if (!temp.GetComponent<Rigidbody2D>())
            {
                temp.AddComponent<Rigidbody2D>();       
                temp.GetComponent<Rigidbody2D>().gravityScale = 0;  
            }
            snakeBody.Add(temp);    // snakeBody 리스트에 몸통 추가
            bodyParts.RemoveAt(0);  // 인덱스로 요소 제거
            temp.GetComponent<MarkerManager>().ClearMarkerList();   // 해당 몸체에 대한 초기화
            countUp = 0;
        }
    }

    void SnakeMovement()    // 이동 처리
    {
        snakeBody[0].GetComponent<Rigidbody2D>().velocity = snakeBody[0].transform.right * speed * Time.deltaTime;  // 머리의 움직임 설정 (로컬 오른쪽 방향)
        if (Input.GetAxis("Horizontal") != 0)
            snakeBody[0].transform.Rotate(Vector3.back * turnSpeed * Time.deltaTime * Input.GetAxis("Horizontal"));

        if (snakeBody.Count > 1)
        {
            for (int i = 1; i < snakeBody.Count; i++)
            {
                MarkerManager markM = snakeBody[i - 1].GetComponent<MarkerManager>();   // 앞에 위치한 몸체의 마커 불러오기
                snakeBody[i].transform.position = markM.markerList[0].position;         // 앞에 위치한 몸체의 마커 리스트에서 맨 앞 마커를 참조
                snakeBody[i].transform.rotation = markM.markerList[0].rotation;
                markM.markerList.RemoveAt(0);   // 마커 리스트의 첫번째 요소를 제거하면서 순차적으로 마커를 참조하여 이동
                
            }

            if (bodyParts.Count == 0)
            {
                MarkerManager lastMarkM = snakeBody[snakeBody.Count - 1].GetComponent<MarkerManager>();     // 마지막에 위치한 몸체의 마커 불러오기
                lastMarkM.enabled = false;  // 마지막에 위치한 몸체의 리스트는 업데이트 하지 않도록 비활성화
            }
        }
    }

    void AddBodyParts(GameObject obj)    // 새로운 몸체 추가
    {
        MarkerManager lastMarkM = snakeBody[snakeBody.Count - 1].GetComponent<MarkerManager>();
        lastMarkM.enabled = true;   // 마지막 몸체의 MarkerManager 재활성화 (새로운 몸체를 생성할때 참조할 마커에 접근하기 위해서)
        bodyParts.Add(obj);
    }

    // 경로가 닫혔을 때 처리하는 함수
    void CloseTrail()
    {
        trailClosed = true;     // 경로가 닫혔음을 표시

        polygonCollider.SetPath(0, points.ToArray());           // 다각형 콜라이더로 경로 설정
        polygonCollider.enabled = true;                         // 콜라이더 활성화

        //Debug.Log(polygonCollider.pathCount); // 경로 개수 확인
        //Debug.Log(polygonCollider.GetPath(0).Length); // 점 개수 확인

        //Invoke("ClearTrail", 0.2f);            // 인보크로 경로와 콜라이더 초기화
        StartCoroutine(ClearTrailRoutine());     // 코루틴으로 경로와 콜라이더 초기화
    }

    IEnumerator ClearTrailRoutine()
    {
        //yield return new WaitForEndOfFrame(); // 현재 프레임이 모두 끝날때까지 대기
        yield return null;  // 1프레임 대기

        ClearTrail();
    }

    // 라인과 콜라이더를 초기화하는 함수
    void ClearTrail()
    {
        lineRenderer.positionCount = 0;         // 라인 렌더러 초기화 (라인 지우기)

        polygonCollider.enabled = false;        // 콜라이더 경로 초기화 (콜라이더 비활성화)
        
        points.Clear();                         // 경로 좌표 리스트 초기화

        trailClosed = false;                    // 경로가 닫힌 상태 초기화
    }

    // 경로의 총 길이를 계산하는 함수
    float CalculatePathLength()
    {
        float totalLength = 0.0f;

        for (int i = 1; i < points.Count; i++)
        {
            totalLength += Vector2.Distance(points[i - 1], points[i]);
        }

        return totalLength;
    }
}
