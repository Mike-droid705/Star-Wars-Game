using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretAccuracy : MonoBehaviour
{

	[SerializeField] public float m_cannonSpread;
	[SerializeField] public float m_cannonFireRate;
	[SerializeField] float m_inaccuracyLevel;

    public void ChangeAccuracy(){
		m_cannonSpread=m_cannonSpread*m_inaccuracyLevel*2;
		m_cannonFireRate=m_cannonFireRate/m_inaccuracyLevel;
	}
}
