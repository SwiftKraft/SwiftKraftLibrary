using Player.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PL_Voice : MonoBehaviour
{
    [SerializeField] AudioSource voiceSource;
    [SerializeField] AudioClip[] strainingVoiceClips;
    [SerializeField] AudioClip[] injuredVoiceClips;
    [SerializeField] AudioClip[] exhaustedVoiceClips;
    [SerializeField] AudioClip deathVoiceClip;
    [SerializeField] AudioClip[] painVoiceClips;
    [SerializeField] AudioClip[] saluteVoiceClips;

    [SerializeField] PL_ResourceManagement res;
    [SerializeField] PlayerMotor mov;
    [SerializeField] PL_GForces gloc;
    [SerializeField] PL_ODM odm;

    [SerializeField] bool isDead = false;

    private void Update()
    {
        if(voiceSource.isPlaying || isDead) { return ; }

        if (res.StrainingCheck() || res.StrainingCheck() && gloc.gForcesAverage > 0.15f)
        {
            voiceSource.pitch = Random.Range(0.9f, 1.1f);
            voiceSource.clip = strainingVoiceClips[Random.Range(0, strainingVoiceClips.Length)];
            voiceSource.Play();
        }
        else if(res.ExhaustedCheck())
        {
            voiceSource.pitch = Random.Range(0.9f, 1.1f);
            voiceSource.clip = exhaustedVoiceClips[Random.Range(0, exhaustedVoiceClips.Length)];
            voiceSource.Play();
        }
        else if (res.InjuredCheck())
        {
            voiceSource.pitch = Random.Range(0.9f, 1.1f);
            voiceSource.clip = injuredVoiceClips[Random.Range(0, injuredVoiceClips.Length)];
            voiceSource.Play();
        }
    }

    public void KillPlayer()
    {
        isDead = true;
        voiceSource.Stop();
        voiceSource.pitch = Random.Range(0.85f, 0.9f);
        voiceSource.clip = deathVoiceClip;
        voiceSource.Play();
    }

    public void HurtPLayer()
    {
        if (voiceSource.isPlaying) return;
        voiceSource.pitch = Random.Range(0.9f, 1.1f);
        voiceSource.clip = painVoiceClips[Random.Range(0, painVoiceClips.Length)];
        voiceSource.Play();
    }

    public void Salute()
    {
        if (voiceSource.isPlaying) return;
        voiceSource.pitch = Random.Range(0.9f, 1.1f);
        voiceSource.clip = saluteVoiceClips[Random.Range(0, saluteVoiceClips.Length)];
        voiceSource.Play();
    }
}
