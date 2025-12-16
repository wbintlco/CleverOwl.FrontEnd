<?php

if(!$_POST['page']) die("0");

$page = $_POST['page'];
if(file_exists($page.'.html'))
echo file_get_contents($page.'.html');

else echo file_get_contents('pages/404.html');
?>
