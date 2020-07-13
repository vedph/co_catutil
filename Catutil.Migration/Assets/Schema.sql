-- Table `line`

DROP TABLE IF EXISTS `line`;
CREATE TABLE `line` (
  `id` varchar(10) NOT NULL,
  `poem` varchar(10) NOT NULL,
  `ordinal` int NOT NULL,
  `value` varchar(300) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Table `fragment`

DROP TABLE IF EXISTS `fragment`;
CREATE TABLE `fragment` (
  `id` int NOT NULL AUTO_INCREMENT,
  `lineId` varchar(10) NOT NULL,
  `ordinal` tinyint NOT NULL,
  `value` varchar(5000) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_fragment_line_idx` (`lineId`),
  CONSTRAINT `fk_fragment_line` FOREIGN KEY (`lineId`) REFERENCES `line` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Table `entry`

DROP TABLE IF EXISTS `entry`;
CREATE TABLE `entry` (
  `id` int NOT NULL AUTO_INCREMENT,
  `fragmentId` int NOT NULL,
  `ordinal` tinyint NOT NULL,
  `value` varchar(1000) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_entry_fragment_idx` (`fragmentId`),
  CONSTRAINT `fk_entry_fragment` FOREIGN KEY (`fragmentId`) REFERENCES `fragment` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

