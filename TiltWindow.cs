using UnityEngine;

public class TiltWindow : MonoBehaviour
{
	public Vector2 range = new Vector2(5f, 3f);
    public float m_Angle = 5f;
	Transform mTrans;
	Quaternion mStart;
	Vector2 mRot = Vector2.zero;
    private const float lowPassFilterFactor = 0.2f;

    private float m_OldTime = 0f;
    private Quaternion m_OldQuat;
	void Start ()
	{
		mTrans = transform;
        mTrans.eulerAngles = new Vector3(0f, m_Angle, 0f);
        mStart = mTrans.localRotation;
        //设置设备陀螺仪的开启/关闭状态，使用陀螺仪功能必须设置为 true  
        Input.gyro.enabled = true;
        //获取设备重力加速度向量  
        //Vector3 deviceGravity = Input.gyro.gravity;
        //设备的旋转速度，返回结果为x，y，z轴的旋转速度，单位为（弧度/秒）  
        //Vector3 rotationVelocity = Input.gyro.rotationRate;
        //获取更加精确的旋转  
        //Vector3 rotationVelocity2 = Input.gyro.rotationRateUnbiased;
        //设置陀螺仪的更新检索时间，即隔 0.1秒更新一次  
        Input.gyro.updateInterval = 1f;
        //获取移除重力加速度后设备的加速度  
        //Vector3 acceleration = Input.gyro.userAcceleration;
        m_OldTime = Time.realtimeSinceStartup;
        
	}

	void Update ()
	{
		/*Vector3 pos = Input.mousePosition;

		float halfWidth = Screen.width * 0.5f;
		float halfHeight = Screen.height * 0.5f;
		float x = Mathf.Clamp((pos.x - halfWidth) / halfWidth, -1f, 1f);
		float y = Mathf.Clamp((pos.y - halfHeight) / halfHeight, -1f, 1f);
		mRot = Vector2.Lerp(mRot, new Vector2(x, y), Time.deltaTime * 5f);

		mTrans.localRotation = mStart * Quaternion.Euler(-mRot.y * range.y, mRot.x * range.x, 0f);*/
        
        //Input.gyro.attitude 返回值为 Quaternion类型，即设备旋转欧拉角  
        /*if ((Input.gyro.attitude == m_OldQuat) && (Time.realtimeSinceStartup - m_OldTime < 10f))
        {
            return;
        }
        m_OldTime = Time.realtimeSinceStartup;
        m_OldQuat = Input.gyro.attitude;*/
        Quaternion quat = Input.gyro.attitude;
        Vector3 eulerAngles = quat.eulerAngles;
        eulerAngles.y = eulerAngles.y + eulerAngles.z;
        eulerAngles.y = Mathf.Clamp(eulerAngles.y, 0.0f, 360.0f);
        Debug.Log("eulerAngles:" + eulerAngles);
        if (eulerAngles.y < 180f)
        {
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, 0f, 10f);
        }
        else
        {
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, 350f, 360f);
        }
        eulerAngles.x = 0f;
        eulerAngles.z = 0f;
        mTrans.localEulerAngles = eulerAngles;
        /*Debug.Log("attitude:" + Input.gyro.attitude);
        Debug.Log("quat.eulerAngles:" + quat.eulerAngles);
        Debug.Log("localEulerAngles:" + mTrans.localEulerAngles);*/
        /*quat.x = Mathf.Clamp(quat.x, -0.2f, 0.2f);
        quat.y = Mathf.Clamp(quat.y, -0.1f, 0.1f);
        quat.z = 0f;
        quat.w = -1f;
        mTrans.rotation = Quaternion.Slerp(mTrans.rotation, quat, lowPassFilterFactor);*/

        /*Debug.Log("attitude:" + Input.gyro.attitude);
        Debug.Log("quat:" + quat);
        Debug.Log("mTrans.rotation:" + mTrans.rotation);*/
    }
}
