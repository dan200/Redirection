#!/bin/sh

read -p "Have you built the game in Release and ReleaseWindows mode? " PROMPT
if [ $PROMPT != "yes" ]
then
	echo "You should do that first."
	exit
fi 

TITLE="Redirection"
VERSION=`monodis --assembly ${TITLE}/bin/Release/${TITLE}.exe | grep Version | egrep -o "[0-9]+.[0-9]+.[0-9]+"`
EXPORT="svn export -q"

echo "Preparing directories"
rm -rf Deploy
mkdir Deploy

function prepare {
	echo "Preparing ${1} files"
	mkdir Deploy/${1}

	# Game
	cp ${TITLE}/SDL2-CS.dll Deploy/${1}
	cp ${TITLE}/MiniTK.dll Deploy/${1}
	cp ${TITLE}/Ionic.Zip.dll Deploy/${1}
	if [[ ${1} =~ "Win" ]]; then
		cp ${TITLE}/bin/ReleaseWindows/${TITLE}.exe Deploy/${1}
	else
		cp ${TITLE}/bin/Release/${TITLE}.exe Deploy/${1}
		cp ${TITLE}/SDL2-CS.dll.config Deploy/${1}
		cp ${TITLE}/MiniTK.dll.config Deploy/${1}
	fi
	$EXPORT ${TITLE}/assets Deploy/${1}/assets

	if [[ ${1} =~ "Win" ]]; then
		# Native DLLS
		$EXPORT ${TITLE}/Natives/${1} Deploy/Temp
		cp -r Deploy/Temp/* Deploy/${1}
		rm -rf Deploy/Temp
	else
		# MonoKickStart
		$EXPORT KickStart Deploy/Temp
		cp -r Deploy/Temp/* Deploy/${1}
		rm -rf Deploy/Temp
		if [[ ${1} =~ "OSX" ]]; then
			rm Deploy/${1}/${TITLE}.bin.x86
			rm Deploy/${1}/${TITLE}.bin.x86_64
		fi
		if [[ ${1} =~ "Linux" ]]; then
			rm Deploy/${1}/${TITLE}.bin.osx
		fi

		# Native DLLS
		if [[ ${1} =~ "OSX" ]]; then
			$EXPORT ${TITLE}/Natives/${1} Deploy/${1}/osx
		fi
		if [[ ${1} =~ "Linux" ]]; then
			$EXPORT ${TITLE}/Natives/${1}32 Deploy/${1}/lib
			$EXPORT ${TITLE}/Natives/${1}64 Deploy/${1}/lib64
		fi
	fi
}

function package_zipped_bundle {
	if [ -d "Deploy/${1}" ]; then
		echo "Packaging for ${1}"
		$EXPORT AppBundle Deploy/${1}Bundle
		cp -r Deploy/${1}/* Deploy/${1}Bundle/${TITLE}.app/Contents/MacOS
		cp ${TITLE}/Icons/Icon.icns Deploy/${1}Bundle/${TITLE}.app/Contents/Resources

		cd Deploy/${1}Bundle
		zip -rq ../${TITLE}_${2}_${VERSION}.zip ${TITLE}.app
        cd ..
        md5 ${TITLE}_${2}_${VERSION}.zip >> md5_hashes.txt
        cd ..
	fi
}

function package_zip {
	if [ -d "Deploy/${1}" ]; then
		echo "Packaging for ${1}"
		cd Deploy/${1}
		zip -rq ../${TITLE}_${2}_${VERSION}.zip *
        cd ..
        md5 ${TITLE}_${2}_${VERSION}.zip >> md5_hashes.txt
        cd ..		
	fi
}

prepare OSX
prepare Win32
prepare Linux

touch Deploy/md5_hashes.txt
package_zipped_bundle OSX OSX
package_zip Win32 Windows
package_zip Linux Linux
