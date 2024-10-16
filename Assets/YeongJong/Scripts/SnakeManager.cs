using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 뱀 움직임 (몸체 여러개 연결 + 추가 + 제거)
public class SnakeManager : MonoBehaviour
{
    [SerializeField] float distanceBetween = 0.2f;  // 몸체 생성 시간차 (신체 부위 간격)
    [SerializeField] float speed = 280;     // 이동 속도
    [SerializeField] float turnSpeed = 180;  // 회전 속도
    [SerializeField] List<GameObject> bodyParts = new List<GameObject>();   // 생성할 몸체 게임오브젝트(프리팹) 리스트
    [SerializeField] List<GameObject> snakeBody = new List<GameObject>();   // 생성된 몸체 게임오브젝트 리스트

    [SerializeField] GameObject[] bodyPartsDynamic;   // 동적으로 추가될 몸체 게임오브젝트(프리펩) 배열

    float countUp = 0;  // 몸체 생성을 위한 누적 시간

    void Start()
    {
        CreateBodyParts(); 
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
}