# FrostBite

## Progolue:

In the past few days I've been experimenting with the [AppDomain manager injection](https://gist.github.com/djhohnstein/afb93a114b848e16facf0b98cd7cb57b) technique had a decent success with it in my previous Red Team engagements against certain EDRs. Although, this is really good for initial access vector, I wanted to release a POC which will help hiding your shellcode elsewhere. No more shellcode embedded DLL files! 

## The Problem! 
Although it is an excellent technique when used independently, but when coupled with a delivery technique like sending a C# ClickOnce inside an ISO/ZIP/VHD/VHDX file. The real problem is 1 out of 10 times the DLL for the appdomain was detected by AI/ML heurestics of the AV/EDR. This is because the DLL file needs to be dropped on the disk before initializing the appdomain. Ignoring the remote DLL loads for the time being (UNC paths in .config), the DLL for the appdomain would contain the shellcode and I strongly felt that is the reason for a probable static detection, because the rest of the code which is WINAPI calls can be dynamically resolved and pretty well obfuscated. 

I wanted to enhance this technique in terms of minimizing what the DLL would initially hold. I started by dropping encrypted shellcode in a separate file on disk along with the injector DLL but then I came across this amazing blog from Checkpoint on [Zloader's Campaign](https://research.checkpoint.com/2022/can-you-trust-a-files-digital-signature-new-zloader-campaign-exploits-microsofts-signature-verification-putting-users-at-risk/) 

TLDR version: We can embed arbitrary data into some fields within the PE in a way that would not break the files Signature. So our data will get embedded and the exe will still remain digitally signed.

More info on this - https://www.blackhat.com/docs/us-16/materials/us-16-Nipravsky-Certificate-Bypass-Hiding-And-Executing-Malware-From-A-Digitally-Signed-Executable-wp.pdf

So the idea is to embed an encrypted shellcode stub into a known signed executable and still manage to keep it signed like how the Zloader malware did. By doing so the AppDomain Manager DLL will no longer contain the shellcode within itself, but will just have the logic to parse the shellcode from the PE binary that loads it to decrypt and execute as a seperate thread. Doing this might decrease the static detection rate for the DLL while your shellcode is nicely placed inside a signed binary.

I was trying to achieve this by manually tampering with the ZLoader samples I got from VirusTotal, but later found about a project which had already implemented all of these techniques pretty well - [Sigflip](https://github.com/med0x2e/SigFlip). In this POC I leveraged Sigflip's loader code to build the AppDomain DLL and SigFlip injector to embed the encrypted shellcode into our C# exe.


## Advantages:  
Large blobs of shellcode like Cobalt Strike's Stageless shellcode will no longer reside on an unsigned DLL on disk, irrespective of the obfuscation / encoding techniques used. The DLL is cleaner, smaller and stealthier with minimal code thereby reducing the changes of detection. 

## Working

![Diagram](https://github.com/pwn1sher/frostbite/blob/main/diagram.PNG)

## Steps to build Signed Shellcode Executable

- Pick any x64 Signed C# binary of your choice, a binary within which you would like cobalt strike beacon to reside and execute: E.g.: CasPol.exe etc.
- Generate your Cobalt Strike Stageless Shellcode - x64-stageless.bin
- Place both of them into a folder where [SigFlip](https://github.com/med0x2e/SigFlip) is also present and run the below command:  
```SigFlip.exe -i "Z:\ZLoader\CasPol.exe" "Z:\ZLoader\x64-stageless.bin" "Z:\ZLoader\update.exe"  "S3cretK3y"```
- Thanks to SigFlip now you have a (windows signed?) binary named `update.exe` which will be a digitally signed PE with encrypted shellcode embedded in it.

## Steps to build the AppDomain Loader DLL

- Take the C# Template Code from [here](https://github.com/pwn1sher/frostbite/blob/main/test.cs) 
- Replace your encryption secret key with the one you chose while running SigFlip at Line:163 (you might have to adjust a few bytes to confirm if your CS shellcode is properly decrypted)
- Replace with the binary path at Line:146
- Change the log file paths in lines: 158,165
- Compile the code as DLL using the following command - `csc /target:library /out:test.dll test.cs`
- Place the compiled DLL and the [update.exe.config](https://github.com/pwn1sher/frostbite/blob/main/Update.exe.config) file in same folder where your signed shellcode exe was placed.
- Execute update.exe.

## Conslusion:

This POC is just an idea I had in mind to club two totally different defense evasive techniques together that would help me and other Red Teamers in building better initial execution payloads for their operations. This project uses AppDomain Manager Injection as an example, but this idea is applicable for other injection techniques as well like - DLL SideLoading, DLL Hijacking etc

# Credits:
Full Credits to [med0x2e](https://github.com/med0x2e/), this POC is built based on his [SigFlip](https://github.com/med0x2e/SigFlip) Project

# References:
- https://research.checkpoint.com/2022/can-you-trust-a-files-digital-signature-new-zloader-campaign-exploits-microsofts-signature-verification-putting-users-at-risk/
- https://www.blackhat.com/docs/us-16/materials/us-16-Nipravsky-Certificate-Bypass-Hiding-And-Executing-Malware-From-A-Digitally-Signed-Executable-wp.pdf
- https://pentestlaboratories.com/2020/05/26/appdomainmanager-injection-and-detection/
- https://github.com/med0x2e/SigFlip
